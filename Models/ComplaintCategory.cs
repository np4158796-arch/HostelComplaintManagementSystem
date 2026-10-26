using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HostelComplaintManagementSystem.Models
{
    public class ComplaintCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string Icon { get; set; } = "bi-tag";

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
    }
}
