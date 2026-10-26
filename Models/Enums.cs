namespace HostelComplaintManagementSystem.Models
{
    public enum ComplaintPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Urgent = 4
    }

    public enum ComplaintStatus
    {
        Submitted = 1,
        UnderReview = 2,
        Assigned = 3,
        InProgress = 4,
        Resolved = 5,
        Closed = 6,
        Reopened = 7
    }

    public enum UserRoleType
    {
        Student,
        Warden,
        MaintenanceStaff
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3
    }

    public enum NotificationType
    {
        ComplaintSubmitted = 1,
        ComplaintAssigned = 2,
        StatusUpdated = 3,
        CommentAdded = 4,
        ComplaintResolved = 5,
        ComplaintReopened = 6,
        SystemAlert = 7
    }

    public enum TaskAssignmentStatus
    {
        Assigned = 1,
        Accepted = 2,
        InProgress = 3,
        Completed = 4,
        Reassigned = 5
    }
}
