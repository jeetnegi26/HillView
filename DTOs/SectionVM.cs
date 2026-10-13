using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class SectionVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Section type is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid section type.")]
        [Display(Name = "Section Type *")]
        public int SectionTypeId { get; set; }

        [Required(ErrorMessage = "Section title is required.")]
        [StringLength(200)]
        [Display(Name = "Section Title *")]
        public string Title { get; set; }

        [StringLength(200)]
        public string Heading { get; set; }

        [StringLength(200)]
        [Display(Name = "Subheading")]
        public string SubHeading { get; set; }

        [StringLength(4000)]
        public string Description { get; set; }

        [StringLength(100)]
        [Display(Name = "Internal Name")]
        public string InternalName { get; set; }

        [StringLength(50)]
        [Display(Name = "Background Color")]
        public string BackgroundColor { get; set; }

        [StringLength(200)]
        [Display(Name = "CSS Class")]
        public string CssClass { get; set; }

        [Display(Name = "Settings JSON")]
        public string SettingsJson { get; set; }

        [Display(Name = "Active")]
        public bool Status { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}