
using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class ButtonVM
    {
        public Guid Id { get; set; }

        [Display(Name = "Section")]
        public Guid? SectionId { get; set; }

        [Display(Name = "Section Item")]
        public Guid? SectionItemId { get; set; }

        [Required(ErrorMessage = "Button title is required.")]
        [StringLength(200)]
        [Display(Name = "Button Title *")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Button URL is required.")]
        [StringLength(1000)]
        [Display(Name = "Button URL *")]
        public string Url { get; set; }

        [StringLength(50)]
        public string Target { get; set; } = "_self";

        [StringLength(200)]
        [Display(Name = "CSS Class")]
        public string CssClass { get; set; }

        [StringLength(100)]
        [Display(Name = "Button Type")]
        public string ButtonType { get; set; }

        [Display(Name = "Display Order")]
        [Range(0, int.MaxValue,
            ErrorMessage = "Display order cannot be negative.")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Active")]
        public bool Status { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
