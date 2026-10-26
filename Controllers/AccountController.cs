using System;
using System.Security.Claims;
using System.Threading.Tasks;
using HostelComplaintManagementSystem.Data;
using HostelComplaintManagementSystem.Models;
using HostelComplaintManagementSystem.Services;
using HostelComplaintManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HostelComplaintManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            ICacheService cacheService,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _cacheService = cacheService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToUserDashboard();
            }

            ViewBag.GoogleConfigured = !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]);
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewBag.GoogleConfigured = !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email address or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact the hostel administration.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in successfully.", model.Email);
                TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToUserDashboard(user);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account {Email} locked out.", model.Email);
                ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed login attempts. Please try again in 5 minutes.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid email address or password.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToUserDashboard();
            }

            var blocks = await _cacheService.GetCachedBlocksAsync();
            ViewBag.Blocks = new SelectList(blocks, "Id", "Name");
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var blocks = await _cacheService.GetCachedBlocksAsync();
            ViewBag.Blocks = new SelectList(blocks, "Id", "Name", model.BlockId);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            // Public registration strictly assigns Student role
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName.Trim(),
                StudentId = model.StudentId.Trim().ToUpper(),
                PhoneNumber = model.PhoneNumber.Trim(),
                BlockId = model.BlockId,
                RoomNumber = model.RoomNumber.Trim().ToUpper(),
                UserRole = UserRoleType.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Student");
                _logger.LogInformation("Student {Email} registered successfully.", user.Email);

                TempData["SuccessMessage"] = "Your registration was successful! Please log in with your credentials.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            _logger.LogInformation("User logged out.");
            TempData["SuccessMessage"] = "You have been logged out securely.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var googleConfigured = !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]);
            if (!googleConfigured)
            {
                TempData["ErrorMessage"] = "Google OAuth is not configured yet. Please configure 'Authentication:Google:ClientId' and 'ClientSecret' in appsettings.json or user-secrets to enable social sign-in.";
                return RedirectToAction(nameof(Login));
            }

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"External login provider error: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ErrorMessage"] = "Error loading external login information.";
                return RedirectToAction(nameof(Login));
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (signInResult.Succeeded)
            {
                TempData["SuccessMessage"] = "Logged in via Google successfully!";
                return RedirectToLocal(returnUrl);
            }

            // Create new student account if first time OAuth
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? "Hostel Student";

            if (!string.IsNullOrEmpty(email))
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    var defaultBlock = await _context.HostelBlocks.FirstOrDefaultAsync();
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = name,
                        StudentId = "OAUTH-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper(),
                        BlockId = defaultBlock?.Id,
                        RoomNumber = "TBD",
                        UserRole = UserRoleType.Student,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (createResult.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Student");
                        await _userManager.AddLoginAsync(user, info);
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        TempData["SuccessMessage"] = "Google account linked and logged in! Please update your room details in Profile.";
                        return RedirectToAction("Index", "Student");
                    }
                }
                else
                {
                    await _userManager.AddLoginAsync(user, info);
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToUserDashboard(user);
                }
            }

            TempData["ErrorMessage"] = "Could not authenticate with Google. Please use email and password.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToUserDashboard(ApplicationUser? user = null)
        {
            if (user == null && User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Warden")) return RedirectToAction("Index", "Warden");
                if (User.IsInRole("MaintenanceStaff")) return RedirectToAction("Index", "Staff");
                return RedirectToAction("Index", "Student");
            }

            if (user != null)
            {
                if (user.UserRole == UserRoleType.Warden) return RedirectToAction("Index", "Warden");
                if (user.UserRole == UserRoleType.MaintenanceStaff) return RedirectToAction("Index", "Staff");
                return RedirectToAction("Index", "Student");
            }

            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToUserDashboard();
        }
    }
}
