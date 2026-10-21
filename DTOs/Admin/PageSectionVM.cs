
using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class PageSectionVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Please select a page.")]
        [Display(Name = "Page")]
        public Guid PageId { get; set; }

        [Required(ErrorMessage = "Please select a section.")]
        [Display(Name = "Section")]
        public Guid SectionId { get; set; }

        [Display(Name = "Page Title")]
        public string PageTitle { get; set; }

        [Display(Name = "Section Title")]
        public string SectionTitle { get; set; }

        [Display(Name = "Display Order")]
        [Range(0, int.MaxValue, ErrorMessage = "Display order cannot be negative.")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Active")]
        public bool Status { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
