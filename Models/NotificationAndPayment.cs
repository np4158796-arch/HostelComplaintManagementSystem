using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelComplaintManagementSystem.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public int? ComplaintId { get; set; }
        public virtual Complaint? Complaint { get; set; }

        public NotificationType Type { get; set; } = NotificationType.SystemAlert;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class EmailNotificationLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string RecipientEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsSuccess { get; set; } = true;

        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        [StringLength(50)]
        public string EventType { get; set; } = "General";
    }

    public class HostelPayment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;
        public virtual ApplicationUser? Student { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(1, 100000)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Fee Type")]
        public string FeeType { get; set; } = "Hostel Maintenance Fee";

        [StringLength(250)]
        public string Description { get; set; } = "Annual maintenance and amenities contribution";

        [Required]
        [StringLength(50)]
        public string TransactionId { get; set; } = string.Empty;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public PaymentStatus Status { get; set; } = PaymentStatus.Completed;

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Demo UPI / Card";
    }
}
