using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelComplaintManagementSystem.Models
{
    public class Complaint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Complaint Reference")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Display(Name = "Complaint Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        [Display(Name = "Detailed Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        public virtual ComplaintCategory? Category { get; set; }

        [Required]
        [Display(Name = "Priority Level")]
        public ComplaintPriority Priority { get; set; } = ComplaintPriority.Medium;

        [Required]
        [Display(Name = "Status")]
        public ComplaintStatus Status { get; set; } = ComplaintStatus.Submitted;

        [Required]
        public string StudentId { get; set; } = string.Empty;
        public virtual ApplicationUser? Student { get; set; }

        public int? BlockId { get; set; }
        public virtual HostelBlock? HostelBlock { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Room Number")]
        public string RoomNumber { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Evidence Image")]
        public string? ImageAttachmentUrl { get; set; }

        public string? AssignedStaffId { get; set; }
        public virtual ApplicationUser? AssignedStaff { get; set; }

        [StringLength(1000)]
        [Display(Name = "Administrative Response")]
        public string? AdminResponse { get; set; }

        [StringLength(1000)]
        [Display(Name = "Resolution Notes")]
        public string? ResolutionNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        // Navigation Collections
        public virtual ICollection<ComplaintStatusHistory> StatusHistories { get; set; } = new List<ComplaintStatusHistory>();
        public virtual ICollection<ComplaintComment> Comments { get; set; } = new List<ComplaintComment>();
        public virtual ICollection<ComplaintAssignment> Assignments { get; set; } = new List<ComplaintAssignment>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
