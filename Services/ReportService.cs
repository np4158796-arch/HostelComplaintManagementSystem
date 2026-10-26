using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HostelComplaintManagementSystem.Data;
using HostelComplaintManagementSystem.Models;
using HostelComplaintManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HostelComplaintManagementSystem.Services
{
    public interface IReportService
    {
        Task<WardenDashboardViewModel> GetWardenDashboardStatsAsync();
        Task<ReportSummaryViewModel> GenerateReportSummaryAsync(DateTime? startDate, DateTime? endDate, int? categoryId);
        Task<byte[]> ExportComplaintsToCsvAsync(DateTime? startDate, DateTime? endDate, int? categoryId, ComplaintStatus? status);
    }

    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<WardenDashboardViewModel> GetWardenDashboardStatsAsync()
        {
            var total = await _context.Complaints.CountAsync();
            var pending = await _context.Complaints.CountAsync(c => c.Status == ComplaintStatus.Submitted || c.Status == ComplaintStatus.UnderReview);
            var inProgress = await _context.Complaints.CountAsync(c => c.Status == ComplaintStatus.Assigned || c.Status == ComplaintStatus.InProgress || c.Status == ComplaintStatus.Reopened);
            var resolved = await _context.Complaints.CountAsync(c => c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed);

            // Overdue: Submitted more than 3 days ago and still pending or in progress
            var overdueCutoff = DateTime.UtcNow.AddDays(-3);
            var overdue = await _context.Complaints.CountAsync(c =>
                c.CreatedAt < overdueCutoff &&
                c.Status != ComplaintStatus.Resolved &&
                c.Status != ComplaintStatus.Closed);

            var urgent = await _context.Complaints.CountAsync(c =>
                c.Priority == ComplaintPriority.Urgent &&
                c.Status != ComplaintStatus.Resolved &&
                c.Status != ComplaintStatus.Closed);

            var urgentList = await _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.Category)
                .Include(c => c.HostelBlock)
                .Where(c => c.Priority == ComplaintPriority.Urgent && c.Status != ComplaintStatus.Resolved && c.Status != ComplaintStatus.Closed)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentComplaints = await _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.Category)
                .Include(c => c.HostelBlock)
                .OrderByDescending(c => c.CreatedAt)
                .Take(6)
                .ToListAsync();

            var recentActivities = await _context.ComplaintStatusHistories
                .Include(h => h.Complaint)
                .Include(h => h.ChangedBy)
                .OrderByDescending(h => h.ChangedAt)
                .Take(6)
                .ToListAsync();

            // Category breakdown for Chart.js
            var categoryStats = await _context.Complaints
                .Include(c => c.Category)
                .GroupBy(c => c.Category != null ? c.Category.Name : "Uncategorized")
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            // Monthly trends for past 6 months
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
            var monthlyData = await _context.Complaints
                .Where(c => c.CreatedAt >= new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1))
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            // Status counts
            var statusCounts = await _context.Complaints
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var model = new WardenDashboardViewModel
            {
                TotalComplaints = total,
                PendingComplaints = pending,
                InProgressComplaints = inProgress,
                ResolvedComplaints = resolved,
                OverdueComplaints = overdue,
                UrgentComplaints = urgent,
                UrgentComplaintsList = urgentList,
                RecentComplaintsList = recentComplaints,
                RecentActivities = recentActivities,
                CategoryLabels = categoryStats.Select(c => c.Label).ToList(),
                CategoryCounts = categoryStats.Select(c => c.Count).ToList(),
                StatusLabels = statusCounts.Select(s => s.Status.ToString()).ToList(),
                StatusCounts = statusCounts.Select(s => s.Count).ToList()
            };

            // Build human-friendly month labels (e.g. May 2026, Jun 2026)
            for (int i = 5; i >= 0; i--)
            {
                var dt = DateTime.UtcNow.AddMonths(-i);
                var label = dt.ToString("MMM yyyy");
                var match = monthlyData.FirstOrDefault(m => m.Year == dt.Year && m.Month == dt.Month);
                model.MonthlyLabels.Add(label);
                model.MonthlyCounts.Add(match?.Count ?? 0);
            }

            return model;
        }

        public async Task<ReportSummaryViewModel> GenerateReportSummaryAsync(DateTime? startDate, DateTime? endDate, int? categoryId)
        {
            var query = _context.Complaints
                .Include(c => c.Category)
                .Include(c => c.AssignedStaff)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(c => c.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(c => c.CreatedAt <= endOfDay);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
            }

            var complaints = await query.ToListAsync();
            var total = complaints.Count;
            var resolved = complaints.Count(c => c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed);
            var pending = total - resolved;

            // Average resolution time in hours
            var resolvedWithTime = complaints
                .Where(c => (c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed) && c.ResolvedAt.HasValue)
                .ToList();

            double avgHours = 0;
            if (resolvedWithTime.Any())
            {
                avgHours = Math.Round(resolvedWithTime.Average(c => (c.ResolvedAt!.Value - c.CreatedAt).TotalHours), 1);
            }

            // Categories stats
            var categoryStats = complaints
                .GroupBy(c => c.Category != null ? c.Category.Name : "Other")
                .Select(g => new CategoryReportItem
                {
                    CategoryName = g.Key,
                    Count = g.Count(),
                    Percentage = total > 0 ? Math.Round((double)g.Count() / total * 100, 1) : 0
                })
                .OrderByDescending(c => c.Count)
                .ToList();

            // Status stats
            var statusStats = complaints
                .GroupBy(c => c.Status.ToString())
                .Select(g => new StatusReportItem
                {
                    StatusName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            // Priority stats
            var priorityStats = complaints
                .GroupBy(c => c.Priority.ToString())
                .Select(g => new PriorityReportItem
                {
                    PriorityName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(p => p.Count)
                .ToList();

            // Staff Performance
            var staffList = await _context.Users
                .Where(u => u.UserRole == UserRoleType.MaintenanceStaff)
                .ToListAsync();

            var staffPerformance = new List<StaffPerformanceItem>();
            foreach (var staff in staffList)
            {
                var staffComplaints = complaints.Where(c => c.AssignedStaffId == staff.Id).ToList();
                var completed = staffComplaints.Count(c => c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed);
                var pendingStaff = staffComplaints.Count - completed;

                double staffAvgHours = 0;
                var staffResolved = staffComplaints.Where(c => c.ResolvedAt.HasValue).ToList();
                if (staffResolved.Any())
                {
                    staffAvgHours = Math.Round(staffResolved.Average(c => (c.ResolvedAt!.Value - c.CreatedAt).TotalHours), 1);
                }

                staffPerformance.Add(new StaffPerformanceItem
                {
                    StaffName = staff.FullName,
                    Email = staff.Email ?? "N/A",
                    TotalAssigned = staffComplaints.Count,
                    Completed = completed,
                    Pending = pendingStaff,
                    AverageHoursToComplete = staffAvgHours
                });
            }

            // Monthly trends
            var monthlyTrends = complaints
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Select(g => new MonthlyTrendItem
                {
                    MonthLabel = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    TotalSubmitted = g.Count(),
                    TotalResolved = g.Count(c => c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed)
                })
                .OrderBy(m => m.MonthLabel)
                .ToList();

            return new ReportSummaryViewModel
            {
                TotalComplaints = total,
                ResolvedComplaints = resolved,
                PendingComplaints = pending,
                AverageResolutionHours = avgHours,
                CategoryStats = categoryStats,
                StatusStats = statusStats,
                PriorityStats = priorityStats,
                StaffPerformance = staffPerformance,
                MonthlyTrends = monthlyTrends
            };
        }

        public async Task<byte[]> ExportComplaintsToCsvAsync(DateTime? startDate, DateTime? endDate, int? categoryId, ComplaintStatus? status)
        {
            var query = _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.Category)
                .Include(c => c.HostelBlock)
                .Include(c => c.AssignedStaff)
                .AsQueryable();

            if (startDate.HasValue) query = query.Where(c => c.CreatedAt >= startDate.Value);
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(c => c.CreatedAt <= endOfDay);
            }
            if (categoryId.HasValue && categoryId.Value > 0) query = query.Where(c => c.CategoryId == categoryId.Value);
            if (status.HasValue) query = query.Where(c => c.Status == status.Value);

            var list = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Reference,Title,Category,Priority,Status,Student Name,Student ID,Block,Room,Created At,Resolved At,Assigned Staff");

            foreach (var c in list)
            {
                var title = EscapeCsv(c.Title);
                var cat = EscapeCsv(c.Category?.Name ?? "Uncategorized");
                var studentName = EscapeCsv(c.Student?.FullName ?? "N/A");
                var studentId = EscapeCsv(c.Student?.StudentId ?? "N/A");
                var block = EscapeCsv(c.HostelBlock?.Name ?? "N/A");
                var staff = EscapeCsv(c.AssignedStaff?.FullName ?? "Unassigned");
                var created = c.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                var resolved = c.ResolvedAt.HasValue ? c.ResolvedAt.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "N/A";

                sb.AppendLine($"{c.ReferenceNumber},{title},{cat},{c.Priority},{c.Status},{studentName},{studentId},{block},{c.RoomNumber},{created},{resolved},{staff}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }
}
