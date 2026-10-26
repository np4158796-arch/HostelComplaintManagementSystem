using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelComplaintManagementSystem.Models
{
    public class ComplaintAssignment
    {
        [Key]
        public int Id { get; set; }

        public int ComplaintId { get; set; }
        public virtual Complaint? Complaint { get; set; }

        [Required]
        public string AssignedStaffId { get; set; } = string.Empty;
        public virtual ApplicationUser? AssignedStaff { get; set; }

        [Required]
        public string AssignedById { get; set; } = string.Empty;
        public virtual ApplicationUser? AssignedBy { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public TaskAssignmentStatus Status { get; set; } = TaskAssignmentStatus.Assigned;
    }

    public class ComplaintStatusHistory
    {
        [Key]
        public int Id { get; set; }

        public int ComplaintId { get; set; }
        public virtual Complaint? Complaint { get; set; }

        public ComplaintStatus PreviousStatus { get; set; }

        public ComplaintStatus NewStatus { get; set; }

        [Required]
        public string ChangedById { get; set; } = string.Empty;
        public virtual ApplicationUser? ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class ComplaintComment
    {
        [Key]
        public int Id { get; set; }

        public int ComplaintId { get; set; }
        public virtual Complaint? Complaint { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(1000)]
        public string CommentText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsInternal { get; set; } = false; // Internal admin note vs public comment visible to student
    }
}
