using System;
using System.Threading.Tasks;
using HostelComplaintManagementSystem.Services;
using HostelComplaintManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HostelComplaintManagementSystem.Controllers
{
    [Authorize(Roles = "Warden")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly ICacheService _cacheService;

        public ReportsController(IReportService reportService, ICacheService cacheService)
        {
            _reportService = reportService;
            _cacheService = cacheService;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, int? categoryId)
        {
            var summary = await _reportService.GenerateReportSummaryAsync(startDate, endDate, categoryId);
            var categories = await _cacheService.GetCachedCategoriesAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            var model = new ReportFilterViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                CategoryId = categoryId,
                Summary = summary
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCsv(DateTime? startDate, DateTime? endDate, int? categoryId)
        {
            var csvBytes = await _reportService.ExportComplaintsToCsvAsync(startDate, endDate, categoryId, null);
            var fileName = $"HostelCare_Report_{DateTime.UtcNow:yyyyMMdd}.csv";
            return File(csvBytes, "text/csv", fileName);
        }
    }
}
