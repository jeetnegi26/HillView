using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class PageVM
    {
        public Guid Id { get; set; }


        [Required(ErrorMessage = "Page title is required.")]
        [StringLength(200)]
        [Display(Name = "Page Title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Page URL slug is required.")]
        [StringLength(250)]
        [RegularExpression(
            @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
            ErrorMessage = "Use lowercase letters, numbers, and hyphens only.")]
        [Display(Name = "URL Slug")]
        public string Slug { get; set; }



        [StringLength(200)]
        public string MetaTitle { get; set; }

        [StringLength(500)]
        public string MetaDescription { get; set; }

        [StringLength(1000)]
        public string MetaKeywords { get; set; }

        [StringLength(500)]
        public string CanonicalUrl { get; set; }

        [Display(Name = "Published")]
        public bool Status { get; set; } = false;

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}