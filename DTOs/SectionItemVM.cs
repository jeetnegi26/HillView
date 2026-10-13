
using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class SectionItemVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Section is required.")]
        [Display(Name = "Section *")]
        public Guid SectionId { get; set; }

        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(200)]
        public string Heading { get; set; }

        [StringLength(200)]
        [Display(Name = "Subheading")]
        public string SubHeading { get; set; }

        [StringLength(4000)]
        public string Description { get; set; }

        [StringLength(500)]
        public string Value { get; set; }

        [StringLength(200)]
        public string Label { get; set; }

        [StringLength(1000)]
        [Display(Name = "Link URL")]
        public string LinkUrl { get; set; }

        [Display(Name = "Additional Data")]
        public string AdditionalData { get; set; }

        [Display(Name = "Display Order")]
        [Range(0, int.MaxValue, ErrorMessage = "Display order cannot be negative.")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Active")]
        public bool Status { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
