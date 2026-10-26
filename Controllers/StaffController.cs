using System;
using System.Linq;
using System.Threading.Tasks;
using HostelComplaintManagementSystem.Data;
using HostelComplaintManagementSystem.Models;
using HostelComplaintManagementSystem.Services;
using HostelComplaintManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HostelComplaintManagementSystem.Controllers
{
    [Authorize(Roles = "MaintenanceStaff")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IComplaintService _complaintService;
        private readonly ILogger<StaffController> _logger;

        public StaffController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IComplaintService complaintService,
            ILogger<StaffController> logger)
        {
            _context = context;
            _userManager = userManager;
            _complaintService = complaintService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var staffId = user.Id;

            var activeTasks = await _context.Complaints
                .Include(c => c.Category)
                .Include(c => c.HostelBlock)
                .Include(c => c.Student)
                .Where(c => c.AssignedStaffId == staffId && c.Status != ComplaintStatus.Resolved && c.Status != ComplaintStatus.Closed)
                .OrderByDescending(c => c.Priority)
                .ThenBy(c => c.CreatedAt)
                .ToListAsync();

            var completedTasks = await _context.Complaints
                .Include(c => c.Category)
                .Include(c => c.HostelBlock)
                .Include(c => c.Student)
                .Where(c => c.AssignedStaffId == staffId && (c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed))
                .OrderByDescending(c => c.ResolvedAt)
                .Take(10)
                .ToListAsync();

            var totalAssigned = await _context.Complaints.CountAsync(c => c.AssignedStaffId == staffId);
            var inProgress = activeTasks.Count(c => c.Status == ComplaintStatus.InProgress);
            var urgent = activeTasks.Count(c => c.Priority == ComplaintPriority.Urgent);

            var model = new StaffDashboardViewModel
            {
                StaffName = user.FullName,
                StaffEmail = user.Email ?? string.Empty,
                TotalAssigned = totalAssigned,
                InProgressTasks = inProgress,
                CompletedTasks = completedTasks.Count,
                UrgentTasks = urgent,
                ActiveTasks = activeTasks,
                CompletedTasksList = completedTasks
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptTask(int complaintId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == complaintId && c.AssignedStaffId == user.Id);
            if (complaint == null)
            {
                TempData["ErrorMessage"] = "Task not found or not assigned to you.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _complaintService.UpdateStatusAsync(
                complaintId,
                ComplaintStatus.InProgress,
                user.Id,
                "Task accepted by technician and work commenced.",
                null);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Task {complaint.ReferenceNumber} accepted and marked In Progress.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProgress(int complaintId, ComplaintStatus newStatus, string? notes, string? resolutionNotes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == complaintId && c.AssignedStaffId == user.Id);
            if (complaint == null)
            {
                TempData["ErrorMessage"] = "Unauthorized: You may only update tasks assigned directly to you.";
                return RedirectToAction(nameof(Index));
            }

            var noteToRecord = string.IsNullOrWhiteSpace(resolutionNotes) ? notes : resolutionNotes;

            var result = await _complaintService.UpdateStatusAsync(
                complaintId,
                newStatus,
                user.Id,
                noteToRecord,
                null);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Complaint {complaint.ReferenceNumber} updated to {newStatus}.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
