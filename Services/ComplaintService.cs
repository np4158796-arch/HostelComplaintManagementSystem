using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HostelComplaintManagementSystem.Data;
using HostelComplaintManagementSystem.Models;
using HostelComplaintManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HostelComplaintManagementSystem.Services
{
    public interface IComplaintService
    {
        Task<string> GenerateReferenceNumberAsync();
        Task<Complaint> SubmitComplaintAsync(ComplaintCreateViewModel model, string studentId, string? imageUrl);
        Task<(bool Success, string? ErrorMessage)> UpdateStatusAsync(int complaintId, ComplaintStatus newStatus, string changedById, string? notes, string? adminResponse);
        Task<(bool Success, string? ErrorMessage)> AssignStaffAsync(int complaintId, string staffId, string assignedById, string? notes);
        Task<(bool Success, string? ErrorMessage)> AddCommentAsync(int complaintId, string userId, string commentText, bool isInternal);
        Task<(bool Success, string? ErrorMessage)> ReopenComplaintAsync(int complaintId, string studentId, string reason);
        Task<ComplaintDetailsViewModel?> GetComplaintDetailsAsync(int complaintId, string currentUserId, bool isStaffOrWarden);
        Task<ComplaintFilterViewModel> GetFilteredComplaintsAsync(ComplaintFilterViewModel filter, string? studentId = null, string? staffId = null);
        Task<bool> CanTransitionStatusAsync(ComplaintStatus currentStatus, ComplaintStatus newStatus);
    }

    public class ComplaintService : IComplaintService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ComplaintService> _logger;
        private static readonly object _refLock = new();

        public ComplaintService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<ComplaintService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<string> GenerateReferenceNumberAsync()
        {
            int year = DateTime.UtcNow.Year;
            var prefix = $"HC-{year}-";

            // Find max sequence for current year in database
            var latestComplaint = await _context.Complaints
                .AsNoTracking()
                .Where(c => c.ReferenceNumber.StartsWith(prefix))
                .OrderByDescending(c => c.Id)
                .Select(c => c.ReferenceNumber)
                .FirstOrDefaultAsync();

            int nextSeq = 1;
            if (!string.IsNullOrEmpty(latestComplaint))
            {
                var parts = latestComplaint.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int parsed))
                {
                    nextSeq = parsed + 1;
                }
            }

            return $"{prefix}{nextSeq:D4}";
        }

        public async Task<Complaint> SubmitComplaintAsync(ComplaintCreateViewModel model, string studentId, string? imageUrl)
        {
            var student = await _context.Users.FindAsync(studentId);
            if (student == null)
            {
                throw new InvalidOperationException("Student not found.");
            }

            var refNumber = await GenerateReferenceNumberAsync();

            var complaint = new Complaint
            {
                ReferenceNumber = refNumber,
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                CategoryId = model.CategoryId,
                Priority = model.Priority,
                Status = ComplaintStatus.Submitted,
                StudentId = studentId,
                BlockId = model.BlockId,
                RoomNumber = model.RoomNumber.Trim(),
                ImageAttachmentUrl = imageUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            // Record initial status history
            var history = new ComplaintStatusHistory
            {
                ComplaintId = complaint.Id,
                PreviousStatus = ComplaintStatus.Submitted,
                NewStatus = ComplaintStatus.Submitted,
                ChangedById = studentId,
                ChangedAt = DateTime.UtcNow,
                Notes = "Complaint submitted by student."
            };
            _context.ComplaintStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            // In-App Notification to Student
            await _notificationService.CreateNotificationAsync(
                studentId,
                "Complaint Registered",
                $"Your complaint '{complaint.Title}' has been registered with reference {complaint.ReferenceNumber}.",
                complaint.Id,
                NotificationType.ComplaintSubmitted);

            // Optional Email Notification
            if (!string.IsNullOrEmpty(student.Email))
            {
                var emailHtml = $@"<h3>Complaint Registered Successfully</h3>
<p>Dear {student.FullName},</p>
<p>Your hostel complaint has been registered with Reference ID: <strong>{complaint.ReferenceNumber}</strong>.</p>
<p><strong>Title:</strong> {complaint.Title}<br/>
<strong>Priority:</strong> {complaint.Priority}<br/>
<strong>Room:</strong> {complaint.RoomNumber}</p>
<p>Our hostel administration and maintenance team will review it promptly.</p>";

                await _emailService.SendEmailAsync(student.Email, $"HostelCare: Complaint Submitted ({complaint.ReferenceNumber})", emailHtml, "ComplaintSubmitted");
            }

            _logger.LogInformation("Complaint created successfully with Reference {Ref}", complaint.ReferenceNumber);
            return complaint;
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateStatusAsync(
            int complaintId,
            ComplaintStatus newStatus,
            string changedById,
            string? notes,
            string? adminResponse)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Student)
                .FirstOrDefaultAsync(c => c.Id == complaintId);

            if (complaint == null)
            {
                return (false, "Complaint not found.");
            }

            var currentStatus = complaint.Status;
            if (currentStatus == newStatus)
            {
                return (false, "The complaint is already in the selected status.");
            }

            if (!await CanTransitionStatusAsync(currentStatus, newStatus))
            {
                return (false, $"Cannot transition complaint from status '{currentStatus}' to '{newStatus}'.");
            }

            complaint.Status = newStatus;
            complaint.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(adminResponse))
            {
                complaint.AdminResponse = adminResponse.Trim();
            }

            if (newStatus == ComplaintStatus.Resolved || newStatus == ComplaintStatus.Closed)
            {
                complaint.ResolvedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    complaint.ResolutionNotes = notes.Trim();
                }
            }

            var history = new ComplaintStatusHistory
            {
                ComplaintId = complaint.Id,
                PreviousStatus = currentStatus,
                NewStatus = newStatus,
                ChangedById = changedById,
                ChangedAt = DateTime.UtcNow,
                Notes = string.IsNullOrWhiteSpace(notes) ? $"Status changed from {currentStatus} to {newStatus}" : notes.Trim()
            };
            _context.ComplaintStatusHistories.Add(history);

            await _context.SaveChangesAsync();

            // Notify Student
            var notifyType = newStatus == ComplaintStatus.Resolved ? NotificationType.ComplaintResolved : NotificationType.StatusUpdated;
            await _notificationService.CreateNotificationAsync(
                complaint.StudentId,
                $"Complaint Status: {newStatus}",
                $"Your complaint ({complaint.ReferenceNumber}) status was updated to {newStatus}." + (string.IsNullOrWhiteSpace(notes) ? "" : $" Note: {notes}"),
                complaint.Id,
                notifyType);

            // Email update
            if (complaint.Student != null && !string.IsNullOrEmpty(complaint.Student.Email))
            {
                var emailHtml = $@"<h3>Complaint Status Update</h3>
<p>Dear {complaint.Student.FullName},</p>
<p>The status of your complaint <strong>{complaint.ReferenceNumber}</strong> ('{complaint.Title}') has changed to: <strong><span style='color:#4f46e5;'>{newStatus}</span></strong>.</p>
{(string.IsNullOrWhiteSpace(notes) ? "" : $"<p><strong>Remarks:</strong> {notes}</p>")}
<p>Log in to your HostelCare portal to track progress or add feedback.</p>";

                await _emailService.SendEmailAsync(complaint.Student.Email, $"HostelCare: Complaint Status Updated ({complaint.ReferenceNumber})", emailHtml, "StatusUpdated");
            }

            _logger.LogInformation("Complaint {Ref} status updated to {Status} by {User}", complaint.ReferenceNumber, newStatus, changedById);
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> AssignStaffAsync(
            int complaintId,
            string staffId,
            string assignedById,
            string? notes)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Student)
                .FirstOrDefaultAsync(c => c.Id == complaintId);

            if (complaint == null)
            {
                return (false, "Complaint not found.");
            }

            var staff = await _context.Users.FindAsync(staffId);
            if (staff == null)
            {
                return (false, "Selected maintenance staff member not found.");
            }

            var previousStatus = complaint.Status;
            complaint.AssignedStaffId = staffId;
            complaint.UpdatedAt = DateTime.UtcNow;

            // Automatically move to Assigned if currently Submitted or UnderReview
            if (complaint.Status == ComplaintStatus.Submitted || complaint.Status == ComplaintStatus.UnderReview)
            {
                complaint.Status = ComplaintStatus.Assigned;
            }

            var assignment = new ComplaintAssignment
            {
                ComplaintId = complaint.Id,
                AssignedStaffId = staffId,
                AssignedById = assignedById,
                AssignedAt = DateTime.UtcNow,
                Notes = notes,
                Status = TaskAssignmentStatus.Assigned
            };
            _context.ComplaintAssignments.Add(assignment);

            var history = new ComplaintStatusHistory
            {
                ComplaintId = complaint.Id,
                PreviousStatus = previousStatus,
                NewStatus = complaint.Status,
                ChangedById = assignedById,
                ChangedAt = DateTime.UtcNow,
                Notes = $"Assigned to maintenance technician {staff.FullName}. {(string.IsNullOrWhiteSpace(notes) ? "" : $"Note: {notes}")}"
            };
            _context.ComplaintStatusHistories.Add(history);

            await _context.SaveChangesAsync();

            // Notify Staff Member
            await _notificationService.CreateNotificationAsync(
                staffId,
                "New Task Assigned",
                $"You have been assigned complaint {complaint.ReferenceNumber} at Room {complaint.RoomNumber}.",
                complaint.Id,
                NotificationType.ComplaintAssigned);

            // Notify Student
            await _notificationService.CreateNotificationAsync(
                complaint.StudentId,
                "Staff Assigned",
                $"Your complaint ({complaint.ReferenceNumber}) has been assigned to {staff.FullName}.",
                complaint.Id,
                NotificationType.ComplaintAssigned);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> AddCommentAsync(int complaintId, string userId, string commentText, bool isInternal)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Student)
                .FirstOrDefaultAsync(c => c.Id == complaintId);

            if (complaint == null)
            {
                return (false, "Complaint not found.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found.");
            }

            var comment = new ComplaintComment
            {
                ComplaintId = complaintId,
                UserId = userId,
                CommentText = commentText.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsInternal = isInternal
            };

            _context.ComplaintComments.Add(comment);
            await _context.SaveChangesAsync();

            // Notify student if someone else commented and comment is public
            if (!isInternal && userId != complaint.StudentId)
            {
                await _notificationService.CreateNotificationAsync(
                    complaint.StudentId,
                    "New Comment on Complaint",
                    $"{user.FullName} commented on your complaint {complaint.ReferenceNumber}.",
                    complaint.Id,
                    NotificationType.CommentAdded);
            }

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> ReopenComplaintAsync(int complaintId, string studentId, string reason)
        {
            var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == complaintId && c.StudentId == studentId);
            if (complaint == null)
            {
                return (false, "Complaint not found or you are not authorized to reopen it.");
            }

            if (complaint.Status != ComplaintStatus.Resolved && complaint.Status != ComplaintStatus.Closed)
            {
                return (false, "Only resolved or closed complaints can be reopened.");
            }

            var prev = complaint.Status;
            complaint.Status = ComplaintStatus.Reopened;
            complaint.UpdatedAt = DateTime.UtcNow;

            var history = new ComplaintStatusHistory
            {
                ComplaintId = complaint.Id,
                PreviousStatus = prev,
                NewStatus = ComplaintStatus.Reopened,
                ChangedById = studentId,
                ChangedAt = DateTime.UtcNow,
                Notes = $"Complaint reopened by student. Reason: {reason}"
            };
            _context.ComplaintStatusHistories.Add(history);

            await _context.SaveChangesAsync();

            // Notify assigned staff if present
            if (!string.IsNullOrEmpty(complaint.AssignedStaffId))
            {
                await _notificationService.CreateNotificationAsync(
                    complaint.AssignedStaffId,
                    "Complaint Reopened",
                    $"Complaint {complaint.ReferenceNumber} was reopened by the student.",
                    complaint.Id,
                    NotificationType.ComplaintReopened);
            }

            return (true, null);
        }

        public async Task<ComplaintDetailsViewModel?> GetComplaintDetailsAsync(int complaintId, string currentUserId, bool isStaffOrWarden)
        {
            var query = _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.AssignedStaff)
                .Include(c => c.Category)
                .Include(c => c.HostelBlock)
                .Include(c => c.StatusHistories.OrderByDescending(sh => sh.ChangedAt))
                    .ThenInclude(sh => sh.ChangedBy)
                .Include(c => c.Comments.OrderBy(cm => cm.CreatedAt))
                    .ThenInclude(cm => cm.User)
                .AsQueryable();

            var complaint = await query.FirstOrDefaultAsync(c => c.Id == complaintId);
            if (complaint == null) return null;

            // Authorization: If student, ensure it's their complaint
            if (!isStaffOrWarden && complaint.StudentId != currentUserId)
            {
                return null;
            }

            // Filter internal comments if student
            if (!isStaffOrWarden)
            {
                complaint.Comments = complaint.Comments.Where(c => !c.IsInternal).ToList();
            }

            return new ComplaintDetailsViewModel
            {
                Complaint = complaint,
                CanAddComment = true,
                CanUpdateStatus = isStaffOrWarden,
                CanAssignStaff = isStaffOrWarden,
                CanReopen = !isStaffOrWarden && (complaint.Status == ComplaintStatus.Resolved || complaint.Status == ComplaintStatus.Closed)
            };
        }

        public async Task<ComplaintFilterViewModel> GetFilteredComplaintsAsync(ComplaintFilterViewModel filter, string? studentId = null, string? staffId = null)
        {
            var query = _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.Category)
                .Include(c => c.HostelBlock)
                .Include(c => c.AssignedStaff)
                .AsQueryable();

            if (!string.IsNullOrEmpty(studentId))
            {
                query = query.Where(c => c.StudentId == studentId);
            }

            if (!string.IsNullOrEmpty(staffId))
            {
                query = query.Where(c => c.AssignedStaffId == staffId);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(c =>
                    c.ReferenceNumber.ToLower().Contains(term) ||
                    c.Title.ToLower().Contains(term) ||
                    c.RoomNumber.ToLower().Contains(term) ||
                    (c.Student != null && c.Student.FullName.ToLower().Contains(term)));
            }

            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            {
                query = query.Where(c => c.CategoryId == filter.CategoryId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(c => c.Status == filter.Status.Value);
            }

            if (filter.Priority.HasValue)
            {
                query = query.Where(c => c.Priority == filter.Priority.Value);
            }

            if (filter.BlockId.HasValue && filter.BlockId.Value > 0)
            {
                query = query.Where(c => c.BlockId == filter.BlockId.Value);
            }

            if (filter.DateFrom.HasValue)
            {
                query = query.Where(c => c.CreatedAt >= filter.DateFrom.Value);
            }

            if (filter.DateTo.HasValue)
            {
                var endOfDay = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(c => c.CreatedAt <= endOfDay);
            }

            filter.TotalItems = await query.CountAsync();

            filter.Complaints = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return filter;
        }

        public Task<bool> CanTransitionStatusAsync(ComplaintStatus currentStatus, ComplaintStatus newStatus)
        {
            // Business rules for valid complaint workflow transitions
            if (currentStatus == newStatus) return Task.FromResult(true);

            bool isValid = (currentStatus, newStatus) switch
            {
                (ComplaintStatus.Submitted, ComplaintStatus.UnderReview) => true,
                (ComplaintStatus.Submitted, ComplaintStatus.Assigned) => true,
                (ComplaintStatus.Submitted, ComplaintStatus.Closed) => true, // direct reject/duplicate

                (ComplaintStatus.UnderReview, ComplaintStatus.Assigned) => true,
                (ComplaintStatus.UnderReview, ComplaintStatus.InProgress) => true,
                (ComplaintStatus.UnderReview, ComplaintStatus.Closed) => true,

                (ComplaintStatus.Assigned, ComplaintStatus.InProgress) => true,
                (ComplaintStatus.Assigned, ComplaintStatus.Resolved) => true,
                (ComplaintStatus.Assigned, ComplaintStatus.UnderReview) => true,

                (ComplaintStatus.InProgress, ComplaintStatus.Resolved) => true,
                (ComplaintStatus.InProgress, ComplaintStatus.UnderReview) => true,

                (ComplaintStatus.Resolved, ComplaintStatus.Closed) => true,
                (ComplaintStatus.Resolved, ComplaintStatus.Reopened) => true,

                (ComplaintStatus.Closed, ComplaintStatus.Reopened) => true,

                (ComplaintStatus.Reopened, ComplaintStatus.Assigned) => true,
                (ComplaintStatus.Reopened, ComplaintStatus.InProgress) => true,
                (ComplaintStatus.Reopened, ComplaintStatus.Resolved) => true,

                _ => false
            };

            return Task.FromResult(isValid);
        }
    }
}
