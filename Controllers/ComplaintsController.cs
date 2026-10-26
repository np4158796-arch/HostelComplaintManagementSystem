using System;
using System.Security.Claims;
using System.Threading.Tasks;
using HostelComplaintManagementSystem.Data;
using HostelComplaintManagementSystem.Models;
using HostelComplaintManagementSystem.Services;
using HostelComplaintManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HostelComplaintManagementSystem.Controllers
{
    [Authorize]
    public class ComplaintsController : Controller
    {
        private readonly IComplaintService _complaintService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ComplaintsController> _logger;

        public ComplaintsController(
            IComplaintService complaintService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ComplaintsController> logger)
        {
            _complaintService = complaintService;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            bool isStaffOrWarden = User.IsInRole("Warden") || User.IsInRole("MaintenanceStaff");

            var details = await _complaintService.GetComplaintDetailsAsync(id, user.Id, isStaffOrWarden);
            if (details == null)
            {
                TempData["ErrorMessage"] = "Complaint not found or you are not authorized to view it.";
                if (User.IsInRole("Warden")) return RedirectToAction("ManageComplaints", "Warden");
                if (User.IsInRole("MaintenanceStaff")) return RedirectToAction("Index", "Staff");
                return RedirectToAction("MyComplaints", "Student");
            }

            if (isStaffOrWarden)
            {
                var staff = await _context.Users
                    .Where(u => u.UserRole == UserRoleType.MaintenanceStaff && u.IsActive)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
                details.StaffList = new SelectList(staff, "Id", "FullName", details.Complaint.AssignedStaffId);
            }

            return View(details);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(AddCommentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Comment text cannot be empty.";
                return RedirectToAction(nameof(Details), new { id = model.ComplaintId });
            }

            // Only Wardens and Staff can submit internal notes
            bool isStaffOrWarden = User.IsInRole("Warden") || User.IsInRole("MaintenanceStaff");
            bool isInternal = isStaffOrWarden && model.IsInternal;

            var result = await _complaintService.AddCommentAsync(model.ComplaintId, user.Id, model.CommentText, isInternal);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Comment added successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return RedirectToAction(nameof(Details), new { id = model.ComplaintId });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Track(string? refNumber)
        {
            if (string.IsNullOrWhiteSpace(refNumber))
            {
                return View();
            }

            var cleanRef = refNumber.Trim().ToUpperInvariant();
            var complaint = await _context.Complaints
                .Include(c => c.Category)
                .Include(c => c.HostelBlock)
                .Include(c => c.StatusHistories.OrderByDescending(h => h.ChangedAt))
                .FirstOrDefaultAsync(c => c.ReferenceNumber == cleanRef);

            if (complaint == null)
            {
                ViewBag.NotFoundMessage = $"No complaint found with reference '{cleanRef}'. Please verify the ID format (e.g. HC-2026-0001).";
                return View();
            }

            return View(complaint);
        }
    }
}
