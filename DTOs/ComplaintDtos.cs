using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HostelComplaintManagementSystem.Models;

namespace HostelComplaintManagementSystem.DTOs
{
    public class ComplaintResponseDto
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string? BlockName { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string? AssignedStaffName { get; set; }
        public string? ImageAttachmentUrl { get; set; }
        public string? AdminResponse { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class ComplaintCreateDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(150, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Valid Category ID is required")]
        public int CategoryId { get; set; }

        [Required]
        public ComplaintPriority Priority { get; set; } = ComplaintPriority.Medium;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Valid Block ID is required")]
        public int BlockId { get; set; }

        [Required]
        [StringLength(20)]
        public string RoomNumber { get; set; } = string.Empty;
    }

    public class ComplaintUpdateDto
    {
        [Required]
        [StringLength(150, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public ComplaintPriority Priority { get; set; }
    }

    public class ComplaintStatusPatchDto
    {
        [Required]
        public ComplaintStatus NewStatus { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(1000)]
        public string? AdminResponse { get; set; }
    }

    public class ComplaintStatusHistoryDto
    {
        public int Id { get; set; }
        public string PreviousStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string ChangedByName { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class CategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Icon { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
