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

namespace HostelComplaintManagementSystem.Controllers
{
    [Authorize]
    public class PaymentDemoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public PaymentDemoController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var blockName = "Hostel Residence";
            if (user.BlockId.HasValue)
            {
                var block = await _context.HostelBlocks.FindAsync(user.BlockId.Value);
                if (block != null) blockName = block.Name;
            }

            var payments = await _context.HostelPayments
                .Where(p => p.StudentId == user.Id)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.PastPayments = payments;

            var checkoutModel = new PaymentCheckoutViewModel
            {
                Amount = 2500.00m,
                FeeType = "Hostel Maintenance & Amenities Contribution",
                Description = "Semester Amenities & High-Speed Optical Fiber Internet Maintenance",
                StudentName = user.FullName,
                StudentId = user.StudentId ?? "N/A",
                RoomNumber = user.RoomNumber ?? "N/A",
                BlockName = blockName
            };

            return View(checkoutModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessDemoPayment(PaymentCheckoutViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Educational / Syllabus demonstration simulation:
            // Simulates payment gateway webhook / callback verification flow
            bool isSuccess = model.SimulationResult == "Success";
            var txnId = $"TXN-HC-{Guid.NewGuid():N}".Substring(0, 16).ToUpper();

            var payment = new HostelPayment
            {
                StudentId = user.Id,
                Amount = model.Amount > 0 ? model.Amount : 2500.00m,
                FeeType = model.FeeType ?? "Hostel Maintenance Fee",
                Description = model.Description ?? "Maintenance contribution",
                TransactionId = txnId,
                PaymentDate = DateTime.UtcNow,
                Status = isSuccess ? PaymentStatus.Completed : PaymentStatus.Failed,
                PaymentMethod = $"{model.PaymentMethod} (Simulator)"
            };

            _context.HostelPayments.Add(payment);
            await _context.SaveChangesAsync();

            if (isSuccess)
            {
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    "Hostel Fee Receipt",
                    $"Your simulated fee payment of ₹{payment.Amount:N2} was recorded (Txn: {payment.TransactionId}).",
                    null,
                    NotificationType.SystemAlert);

                TempData["SuccessMessage"] = $"[DEMO PAYMENT SUCCESSFUL] Transaction ID: {payment.TransactionId}. Simulated payment of ₹{payment.Amount:N2} was approved.";
            }
            else
            {
                TempData["ErrorMessage"] = $"[DEMO PAYMENT FAILED] Simulated bank transaction failed. Transaction ID: {payment.TransactionId}. No funds were charged.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
