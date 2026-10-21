
using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class SectionTypeVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Section type name is required.")]
        [StringLength(50)]
        [Display(Name = "Section Type Name *")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Display title is required.")]
        [StringLength(100)]
        [Display(Name = "Display Title *")]
        public string Title { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Active")]
        public bool Status { get; set; } = true;

        [Range(0, int.MaxValue,
            ErrorMessage = "Display order must be zero or greater.")]
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }
    }
}
