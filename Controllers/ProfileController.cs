using System;
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

namespace HostelComplaintManagementSystem.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            ICacheService cacheService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            string? blockName = null;
            if (user.BlockId.HasValue)
            {
                var block = await _context.HostelBlocks.FindAsync(user.BlockId.Value);
                blockName = block?.Name;
            }

            var blocks = await _cacheService.GetCachedBlocksAsync();
            ViewBag.Blocks = new SelectList(blocks, "Id", "Name", user.BlockId);

            var model = new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                StudentId = user.StudentId,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                BlockId = user.BlockId,
                BlockName = blockName,
                RoomNumber = user.RoomNumber,
                Role = user.UserRole,
                MemberSince = user.CreatedAt
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateContact(string phoneNumber, string? roomNumber, int? blockId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            user.PhoneNumber = phoneNumber?.Trim();
            if (user.UserRole == UserRoleType.Student)
            {
                if (!string.IsNullOrWhiteSpace(roomNumber)) user.RoomNumber = roomNumber.Trim().ToUpper();
                if (blockId.HasValue) user.BlockId = blockId.Value;
            }
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile details updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update profile details.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all password fields properly.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Your password has been changed successfully.";
            }
            else
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Password change failed: {errors}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
