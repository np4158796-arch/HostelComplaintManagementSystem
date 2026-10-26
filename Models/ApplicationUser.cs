using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace HostelComplaintManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(30)]
        [Display(Name = "Student ID / Employee ID")]
        public string? StudentId { get; set; }

        [StringLength(20)]
        [Display(Name = "Room Number")]
        public string? RoomNumber { get; set; }

        public int? BlockId { get; set; }

        [ForeignKey("BlockId")]
        public virtual HostelBlock? HostelBlock { get; set; }

        [Required]
        [Display(Name = "User Role")]
        public UserRoleType UserRole { get; set; } = UserRoleType.Student;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Complaint> SubmittedComplaints { get; set; } = new List<Complaint>();
        public virtual ICollection<Complaint> AssignedComplaints { get; set; } = new List<Complaint>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<HostelPayment> Payments { get; set; } = new List<HostelPayment>();
    }
}
