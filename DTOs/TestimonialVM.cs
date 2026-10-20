using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class TestimonialVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Client name is required.")]
        [StringLength(150)]
        [Display(Name = "Client Name")]
        public string ClientName { get; set; }

        [Required(ErrorMessage = "Review text is required.")]
        [StringLength(1000)]
        [Display(Name = "Review")]
        public string ReviewText { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; } = 5;

        [Display(Name = "Display Order")]
        [Range(0, int.MaxValue)]
        public int DisplayOrder { get; set; } = 0;

        public bool Status { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; }

        [StringLength(50)]
        public string UserHostAdd { get; set; }
    }
}