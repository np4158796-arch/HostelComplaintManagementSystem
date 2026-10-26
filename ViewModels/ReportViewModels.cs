using System;
using System.Collections.Generic;

namespace HostelComplaintManagementSystem.ViewModels
{
    public class ReportFilterViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CategoryId { get; set; }

        public ReportSummaryViewModel Summary { get; set; } = new ReportSummaryViewModel();
    }

    public class ReportSummaryViewModel
    {
        public int TotalComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public int PendingComplaints { get; set; }
        public double AverageResolutionHours { get; set; }
        public double ResolutionRatePercentage => TotalComplaints > 0 ? Math.Round((double)ResolvedComplaints / TotalComplaints * 100, 1) : 0;

        public List<CategoryReportItem> CategoryStats { get; set; } = new List<CategoryReportItem>();
        public List<StatusReportItem> StatusStats { get; set; } = new List<StatusReportItem>();
        public List<PriorityReportItem> PriorityStats { get; set; } = new List<PriorityReportItem>();
        public List<StaffPerformanceItem> StaffPerformance { get; set; } = new List<StaffPerformanceItem>();
        public List<MonthlyTrendItem> MonthlyTrends { get; set; } = new List<MonthlyTrendItem>();
    }

    public class CategoryReportItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class StatusReportItem
    {
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class PriorityReportItem
    {
        public string PriorityName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class StaffPerformanceItem
    {
        public string StaffName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalAssigned { get; set; }
        public int Completed { get; set; }
        public int Pending { get; set; }
        public double AverageHoursToComplete { get; set; }
    }

    public class MonthlyTrendItem
    {
        public string MonthLabel { get; set; } = string.Empty;
        public int TotalSubmitted { get; set; }
        public int TotalResolved { get; set; }
    }
}
