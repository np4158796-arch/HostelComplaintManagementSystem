using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HostelComplaintManagementSystem.Models
{
    public class Hostel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Hostel Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Hostel Code")]
        public string Code { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Address { get; set; }

        public int TotalRooms { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<HostelBlock> Blocks { get; set; } = new List<HostelBlock>();
    }

    public class HostelBlock
    {
        [Key]
        public int Id { get; set; }

        public int HostelId { get; set; }
        public virtual Hostel? Hostel { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Block Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        public int TotalFloors { get; set; } = 4;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<HostelRoom> Rooms { get; set; } = new List<HostelRoom>();
        public virtual ICollection<ApplicationUser> Residents { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
    }

    public class HostelRoom
    {
        [Key]
        public int Id { get; set; }

        public int BlockId { get; set; }
        public virtual HostelBlock? HostelBlock { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Room Number")]
        public string RoomNumber { get; set; } = string.Empty;

        public int Floor { get; set; } = 1;

        public int Capacity { get; set; } = 2;

        public int CurrentOccupancy { get; set; } = 0;

        public bool IsAvailable => CurrentOccupancy < Capacity;
    }
}
