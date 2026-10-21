
using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class ImageVM
    {
        public Guid Id { get; set; }

        [Display(Name = "Section")]
        public Guid? SectionId { get; set; }

        [Display(Name = "Section Item")]
        public Guid? SectionItemId { get; set; }

        [Required(ErrorMessage = "Image URL is required.")]
        [StringLength(1000)]
        [Display(Name = "Image URL *")]
        public string ImageUrl { get; set; }

        [StringLength(500)]
        [Display(Name = "Alt Text")]
        public string AltText { get; set; }

        [StringLength(500)]
        public string Caption { get; set; }

        [StringLength(100)]
        [Display(Name = "Image Type")]
        public string ImageType { get; set; }

        [Display(Name = "Display Order")]
        [Range(0, int.MaxValue,
            ErrorMessage = "Display order cannot be negative.")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Active")]
        public bool Status { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string SectionTitle { get; set; }
        public string SectionItemTitle { get; set; }
    }
}
