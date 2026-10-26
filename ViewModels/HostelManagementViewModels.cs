using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HostelComplaintManagementSystem.Models;

namespace HostelComplaintManagementSystem.ViewModels
{
    public class UserManagementViewModel
    {
        public IEnumerable<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public string? SearchTerm { get; set; }
        public UserRoleType? RoleFilter { get; set; }
    }

    public class BlockManagementViewModel
    {
        public IEnumerable<HostelBlock> Blocks { get; set; } = new List<HostelBlock>();
        public IEnumerable<HostelRoom> Rooms { get; set; } = new List<HostelRoom>();

        public int NewBlockHostelId { get; set; }

        [Required]
        [StringLength(50)]
        public string NewBlockName { get; set; } = string.Empty;

        public int NewBlockFloors { get; set; } = 4;

        public int NewRoomBlockId { get; set; }

        [Required]
        [StringLength(20)]
        public string NewRoomNumber { get; set; } = string.Empty;

        public int NewRoomFloor { get; set; } = 1;
        public int NewRoomCapacity { get; set; } = 2;
    }

    public class CategoryManagementViewModel
    {
        public IEnumerable<ComplaintCategory> Categories { get; set; } = new List<ComplaintCategory>();

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100)]
        public string NewCategoryName { get; set; } = string.Empty;

        [StringLength(300)]
        public string? NewCategoryDescription { get; set; }

        [Required]
        [StringLength(50)]
        public string NewCategoryIcon { get; set; } = "bi-tag";
    }

    public class PaymentCheckoutViewModel
    {
        public decimal Amount { get; set; } = 2500.00m;
        public string FeeType { get; set; } = "Hostel Maintenance & Amenities Fee";
        public string Description { get; set; } = "Semester Semester Maintenance & High-Speed Wi-Fi Service Contribution";
        public string StudentName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string BlockName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a demo payment method.")]
        public string PaymentMethod { get; set; } = "UPI Demo Simulator";

        public string SimulationResult { get; set; } = "Success"; // Success, Failed
    }

    public class PaymentHistoryViewModel
    {
        public IEnumerable<HostelPayment> Payments { get; set; } = new List<HostelPayment>();
    }
}
