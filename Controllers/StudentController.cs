using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HostelComplaintManagementSystem.Data;
using HostelComplaintManagementSystem.Models;
using HostelComplaintManagementSystem.Services;
using HostelComplaintManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HostelComplaintManagementSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IComplaintService _complaintService;
        private readonly ICacheService _cacheService;
        private readonly IFileStorageService _fileStorageService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<StudentController> _logger;

        public StudentController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IComplaintService complaintService,
            ICacheService cacheService,
            IFileStorageService fileStorageService,
            INotificationService notificationService,
            ILogger<StudentController> logger)
        {
            _userManager = userManager;
            _context = context;
            _complaintService = complaintService;
            _cacheService = cacheService;
            _fileStorageService = fileStorageService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var studentId = user.Id;

            // Load block details
            var blockName = "Unassigned";
            if (user.BlockId.HasValue)
            {
                var block = await _context.HostelBlocks.FindAsync(user.BlockId.Value);
                if (block != null) blockName = block.Name;
            }

            var total = await _context.Complaints.CountAsync(c => c.StudentId == studentId);
            var pending = await _context.Complaints.CountAsync(c => c.StudentId == studentId && (c.Status == ComplaintStatus.Submitted || c.Status == ComplaintStatus.UnderReview));
            var inProgress = await _context.Complaints.CountAsync(c => c.StudentId == studentId && (c.Status == ComplaintStatus.Assigned || c.Status == ComplaintStatus.InProgress || c.Status == ComplaintStatus.Reopened));
            var resolved = await _context.Complaints.CountAsync(c => c.StudentId == studentId && (c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed));

            var recentComplaints = await _context.Complaints
                .Include(c => c.Category)
                .Include(c => c.HostelBlock)
                .Where(c => c.StudentId == studentId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentNotifications = await _notificationService.GetUserNotificationsAsync(studentId, 5);

            var statusCounts = new Dictionary<string, int>
            {
                { "Pending", pending },
                { "In Progress", inProgress },
                { "Resolved", resolved }
            };

            var viewModel = new StudentDashboardViewModel
            {
                StudentName = user.FullName,
                StudentId = user.StudentId,
                BlockName = blockName,
                RoomNumber = user.RoomNumber,
                TotalComplaints = total,
                PendingComplaints = pending,
                InProgressComplaints = inProgress,
                ResolvedComplaints = resolved,
                RecentComplaints = recentComplaints,
                RecentNotifications = recentNotifications,
                StatusDistribution = statusCounts
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> SubmitComplaint()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var categories = await _cacheService.GetCachedCategoriesAsync();
            var blocks = await _cacheService.GetCachedBlocksAsync();

            var model = new ComplaintCreateViewModel
            {
                BlockId = user.BlockId ?? (blocks.FirstOrDefault()?.Id ?? 1),
                RoomNumber = user.RoomNumber ?? string.Empty,
                Categories = new SelectList(categories, "Id", "Name"),
                Blocks = new SelectList(blocks, "Id", "Name", user.BlockId)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComplaint(ComplaintCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var categories = await _cacheService.GetCachedCategoriesAsync();
            var blocks = await _cacheService.GetCachedBlocksAsync();
            model.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
            model.Blocks = new SelectList(blocks, "Id", "Name", model.BlockId);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? imageUrl = null;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadResult = await _fileStorageService.SaveImageAsync(model.ImageFile);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError("ImageFile", uploadResult.ErrorMessage ?? "File upload failed.");
                    return View(model);
                }
                imageUrl = uploadResult.FilePath;
            }

            try
            {
                var complaint = await _complaintService.SubmitComplaintAsync(model, user.Id, imageUrl);
                TempData["SuccessMessage"] = $"Complaint registered successfully with reference {complaint.ReferenceNumber}!";
                return RedirectToAction("Details", "Complaints", new { id = complaint.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting complaint for student {StudentId}", user.Id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while registering your complaint. Please try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> MyComplaints(ComplaintFilterViewModel filter)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Demonstrate Session State (IT5039 Unit II):
            // Remember user's last selected status filter across visits in session
            const string sessionStatusKey = "Student_LastStatusFilter";
            if (filter.Status.HasValue)
            {
                HttpContext.Session.SetString(sessionStatusKey, filter.Status.Value.ToString());
            }
            else if (string.IsNullOrEmpty(filter.SearchTerm) && !filter.CategoryId.HasValue)
            {
                var savedFilter = HttpContext.Session.GetString(sessionStatusKey);
                if (!string.IsNullOrEmpty(savedFilter) && Enum.TryParse<ComplaintStatus>(savedFilter, out var parsedStatus))
                {
                    ViewBag.SessionFilterRestored = true;
                }
            }

            var categories = await _cacheService.GetCachedCategoriesAsync();
            filter.Categories = new SelectList(categories, "Id", "Name", filter.CategoryId);

            var result = await _complaintService.GetFilteredComplaintsAsync(filter, studentId: user.Id);
            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int complaintId, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Please provide a reason for reopening the complaint.";
                return RedirectToAction("Details", "Complaints", new { id = complaintId });
            }

            var result = await _complaintService.ReopenComplaintAsync(complaintId, user.Id, reason.Trim());
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Complaint reopened successfully. The administration has been alerted.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return RedirectToAction("Details", "Complaints", new { id = complaintId });
        }
    }
}
