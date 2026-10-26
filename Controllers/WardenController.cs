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
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HostelComplaintManagementSystem.Controllers
{
    [Authorize(Roles = "Warden")]
    public class WardenController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IComplaintService _complaintService;
        private readonly IReportService _reportService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<WardenController> _logger;

        public WardenController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IComplaintService complaintService,
            IReportService reportService,
            ICacheService cacheService,
            ILogger<WardenController> logger)
        {
            _context = context;
            _userManager = userManager;
            _complaintService = complaintService;
            _reportService = reportService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var dashboardData = await _reportService.GetWardenDashboardStatsAsync();
            return View(dashboardData);
        }

        public async Task<IActionResult> ManageComplaints(ComplaintFilterViewModel filter)
        {
            var categories = await _cacheService.GetCachedCategoriesAsync();
            var blocks = await _cacheService.GetCachedBlocksAsync();

            filter.Categories = new SelectList(categories, "Id", "Name", filter.CategoryId);
            filter.Blocks = new SelectList(blocks, "Id", "Name", filter.BlockId);

            var staffList = await _context.Users
                .Where(u => u.UserRole == UserRoleType.MaintenanceStaff && u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.StaffList = new SelectList(staffList, "Id", "FullName");

            var result = await _complaintService.GetFilteredComplaintsAsync(filter);
            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignStaff(int complaintId, string staffId, string? notes, string? returnUrl = null)
        {
            var warden = await _userManager.GetUserAsync(User);
            if (warden == null) return Challenge();

            var result = await _complaintService.AssignStaffAsync(complaintId, staffId, warden.Id, notes);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Maintenance staff member assigned successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(ManageComplaints));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(ComplaintStatusUpdateViewModel model, string? returnUrl = null)
        {
            var warden = await _userManager.GetUserAsync(User);
            if (warden == null) return Challenge();

            var result = await _complaintService.UpdateStatusAsync(
                model.ComplaintId,
                model.NewStatus,
                warden.Id,
                model.Notes,
                model.AdminResponse);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Complaint status successfully updated to {model.NewStatus}.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(ManageComplaints));
        }

        public async Task<IActionResult> ManageUsers(string? searchTerm, UserRoleType? roleFilter)
        {
            var query = _context.Users
                .Include(u => u.HostelBlock)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(term) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.StudentId != null && u.StudentId.ToLower().Contains(term)) ||
                    (u.RoomNumber != null && u.RoomNumber.ToLower().Contains(term)));
            }

            if (roleFilter.HasValue)
            {
                query = query.Where(u => u.UserRole == roleFilter.Value);
            }

            var users = await query.OrderBy(u => u.UserRole).ThenBy(u => u.FullName).ToListAsync();

            var model = new UserManagementViewModel
            {
                Users = users,
                SearchTerm = searchTerm,
                RoleFilter = roleFilter
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Cannot deactivate oneself
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(ManageUsers));
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User '{user.FullName}' has been {(user.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(ManageUsers));
        }

        public async Task<IActionResult> ManageHostels()
        {
            var blocks = await _context.HostelBlocks
                .Include(b => b.Rooms)
                .Include(b => b.Hostel)
                .OrderBy(b => b.Name)
                .ToListAsync();

            var rooms = await _context.HostelRooms
                .Include(r => r.HostelBlock)
                .OrderBy(r => r.BlockId).ThenBy(r => r.RoomNumber)
                .ToListAsync();

            var hostel = await _context.Hostels.FirstOrDefaultAsync();

            var model = new BlockManagementViewModel
            {
                Blocks = blocks,
                Rooms = rooms,
                NewBlockHostelId = hostel?.Id ?? 1
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBlock(BlockManagementViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.NewBlockName))
            {
                TempData["ErrorMessage"] = "Block name is required.";
                return RedirectToAction(nameof(ManageHostels));
            }

            var hostel = await _context.Hostels.FirstOrDefaultAsync();
            var block = new HostelBlock
            {
                HostelId = hostel?.Id ?? 1,
                Name = model.NewBlockName.Trim(),
                TotalFloors = model.NewBlockFloors,
                IsActive = true
            };

            _context.HostelBlocks.Add(block);
            await _context.SaveChangesAsync();

            // Invalidate cached blocks
            _cacheService.InvalidateBlocksCache();

            TempData["SuccessMessage"] = $"Hostel Block '{block.Name}' added successfully.";
            return RedirectToAction(nameof(ManageHostels));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRoom(BlockManagementViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.NewRoomNumber))
            {
                TempData["ErrorMessage"] = "Room number is required.";
                return RedirectToAction(nameof(ManageHostels));
            }

            var room = new HostelRoom
            {
                BlockId = model.NewRoomBlockId,
                RoomNumber = model.NewRoomNumber.Trim().ToUpper(),
                Floor = model.NewRoomFloor,
                Capacity = model.NewRoomCapacity,
                CurrentOccupancy = 0
            };

            _context.HostelRooms.Add(room);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Room '{room.RoomNumber}' added successfully.";
            return RedirectToAction(nameof(ManageHostels));
        }

        public async Task<IActionResult> ManageCategories()
        {
            var categories = await _context.ComplaintCategories.OrderBy(c => c.Id).ToListAsync();
            var model = new CategoryManagementViewModel
            {
                Categories = categories
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(CategoryManagementViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.NewCategoryName))
            {
                TempData["ErrorMessage"] = "Category name is required.";
                return RedirectToAction(nameof(ManageCategories));
            }

            var category = new ComplaintCategory
            {
                Name = model.NewCategoryName.Trim(),
                Description = model.NewCategoryDescription?.Trim(),
                Icon = string.IsNullOrWhiteSpace(model.NewCategoryIcon) ? "bi-tag" : model.NewCategoryIcon.Trim(),
                IsActive = true
            };

            _context.ComplaintCategories.Add(category);
            await _context.SaveChangesAsync();

            // Invalidate category cache (IT5039 syllabus requirement)
            _cacheService.InvalidateCategoriesCache();

            TempData["SuccessMessage"] = $"Category '{category.Name}' added and cache refreshed!";
            return RedirectToAction(nameof(ManageCategories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategoryStatus(int id)
        {
            var category = await _context.ComplaintCategories.FindAsync(id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction(nameof(ManageCategories));
            }

            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();

            // Invalidate category cache
            _cacheService.InvalidateCategoriesCache();

            TempData["SuccessMessage"] = $"Category '{category.Name}' status updated and cache refreshed.";
            return RedirectToAction(nameof(ManageCategories));
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(DateTime? startDate, DateTime? endDate, int? categoryId, ComplaintStatus? status)
        {
            var csvBytes = await _reportService.ExportComplaintsToCsvAsync(startDate, endDate, categoryId, status);
            var fileName = $"HostelCare_Complaints_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
            return File(csvBytes, "text/csv", fileName);
        }
    }
}
