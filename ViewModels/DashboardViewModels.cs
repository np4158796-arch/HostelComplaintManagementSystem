using System;
using System.Collections.Generic;
using HostelComplaintManagementSystem.Models;

namespace HostelComplaintManagementSystem.ViewModels
{
    public class StudentDashboardViewModel
    {
        public string StudentName { get; set; } = string.Empty;
        public string? StudentId { get; set; }
        public string? BlockName { get; set; }
        public string? RoomNumber { get; set; }
        public int TotalComplaints { get; set; }
        public int PendingComplaints { get; set; }
        public int InProgressComplaints { get; set; }
        public int ResolvedComplaints { get; set; }

        public IEnumerable<Complaint> RecentComplaints { get; set; } = new List<Complaint>();
        public IEnumerable<Notification> RecentNotifications { get; set; } = new List<Notification>();

        public Dictionary<string, int> StatusDistribution { get; set; } = new Dictionary<string, int>();
    }

    public class WardenDashboardViewModel
    {
        public int TotalComplaints { get; set; }
        public int PendingComplaints { get; set; }
        public int InProgressComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public int OverdueComplaints { get; set; }
        public int UrgentComplaints { get; set; }

        public IEnumerable<Complaint> UrgentComplaintsList { get; set; } = new List<Complaint>();
        public IEnumerable<Complaint> RecentComplaintsList { get; set; } = new List<Complaint>();
        public IEnumerable<ComplaintStatusHistory> RecentActivities { get; set; } = new List<ComplaintStatusHistory>();

        // Chart Data Properties (JSON serialized for Chart.js)
        public List<string> CategoryLabels { get; set; } = new List<string>();
        public List<int> CategoryCounts { get; set; } = new List<int>();

        public List<string> MonthlyLabels { get; set; } = new List<string>();
        public List<int> MonthlyCounts { get; set; } = new List<int>();

        public List<string> StatusLabels { get; set; } = new List<string>();
        public List<int> StatusCounts { get; set; } = new List<int>();
    }

    public class StaffDashboardViewModel
    {
        public string StaffName { get; set; } = string.Empty;
        public string StaffEmail { get; set; } = string.Empty;
        public int TotalAssigned { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int UrgentTasks { get; set; }

        public IEnumerable<Complaint> ActiveTasks { get; set; } = new List<Complaint>();
        public IEnumerable<Complaint> CompletedTasksList { get; set; } = new List<Complaint>();
    }
}
