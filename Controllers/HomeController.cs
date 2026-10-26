using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HostelComplaintManagementSystem.Data;
using HostelComplaintManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HostelComplaintManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Demonstrates Output/Response Caching for public landing page (IT5039 Unit III)
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Index()
        {
            // Gather public statistics for landing page counter without exposing any student details
            ViewBag.TotalComplaints = await _context.Complaints.CountAsync();
            ViewBag.ResolvedComplaints = await _context.Complaints.CountAsync(c => c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed);
            ViewBag.TotalRooms = await _context.HostelRooms.CountAsync();
            ViewBag.TotalCategories = await _context.ComplaintCategories.CountAsync(c => c.IsActive);

            return View();
        }

        public IActionResult About()
        {
            ViewBag.Title = "About HostelCare";
            return View();
        }

        public IActionResult Contact()
        {
            ViewBag.Title = "Contact Administration";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        [Route("/Home/AccessDenied")]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View();
        }

        [HttpGet]
        [Route("/Home/NotFound404")]
        public IActionResult NotFound404()
        {
            Response.StatusCode = 404;
            return View();
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
