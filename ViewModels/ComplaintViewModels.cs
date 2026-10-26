using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HostelComplaintManagementSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HostelComplaintManagementSystem.ViewModels
{
    public class ComplaintCreateViewModel
    {
        [Required(ErrorMessage = "Please provide a concise title for your complaint.")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 150 characters.")]
        [Display(Name = "Complaint Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please describe the problem in detail.")]
        [StringLength(2000, MinimumLength = 15, ErrorMessage = "Description must be at least 15 characters long.")]
        [Display(Name = "Detailed Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Please select a priority level.")]
        [Display(Name = "Priority")]
        public ComplaintPriority Priority { get; set; } = ComplaintPriority.Medium;

        [Required(ErrorMessage = "Hostel block is required.")]
        [Display(Name = "Hostel Block")]
        public int BlockId { get; set; }

        [Required(ErrorMessage = "Room number is required.")]
        [StringLength(20, ErrorMessage = "Room number is too long.")]
        [Display(Name = "Room Number")]
        public string RoomNumber { get; set; } = string.Empty;

        [Display(Name = "Upload Photo Evidence (Optional, max 5MB)")]
        public IFormFile? ImageFile { get; set; }

        public IEnumerable<SelectListItem>? Categories { get; set; }
        public IEnumerable<SelectListItem>? Blocks { get; set; }
    }

    public class ComplaintDetailsViewModel
    {
        public Complaint Complaint { get; set; } = null!;
        public bool CanAddComment { get; set; }
        public bool CanUpdateStatus { get; set; }
        public bool CanAssignStaff { get; set; }
        public bool CanReopen { get; set; }
        public AddCommentViewModel NewComment { get; set; } = new AddCommentViewModel();
        public IEnumerable<SelectListItem>? StaffList { get; set; }
    }

    public class ComplaintFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public ComplaintStatus? Status { get; set; }
        public ComplaintPriority? Priority { get; set; }
        public int? BlockId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        public IEnumerable<Complaint> Complaints { get; set; } = new List<Complaint>();
        public IEnumerable<SelectListItem>? Categories { get; set; }
        public IEnumerable<SelectListItem>? Blocks { get; set; }
    }

    public class ComplaintStatusUpdateViewModel
    {
        public int ComplaintId { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public ComplaintStatus CurrentStatus { get; set; }

        [Required(ErrorMessage = "Please select the new status.")]
        public ComplaintStatus NewStatus { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        [StringLength(1000, ErrorMessage = "Response cannot exceed 1000 characters.")]
        public string? AdminResponse { get; set; }
    }

    public class ComplaintAssignViewModel
    {
        public int ComplaintId { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a maintenance staff member.")]
        public string AssignedStaffId { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        public IEnumerable<SelectListItem>? StaffMembers { get; set; }
    }

    public class AddCommentViewModel
    {
        public int ComplaintId { get; set; }

        [Required(ErrorMessage = "Comment text cannot be empty.")]
        [StringLength(1000, MinimumLength = 2, ErrorMessage = "Comment must be between 2 and 1000 characters.")]
        [Display(Name = "Add Note or Comment")]
        public string CommentText { get; set; } = string.Empty;

        [Display(Name = "Internal Note (Only visible to Wardens & Staff)")]
        public bool IsInternal { get; set; } = false;
    }
}
