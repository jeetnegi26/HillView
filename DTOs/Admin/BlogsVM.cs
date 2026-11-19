using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace DTOs
{
    // Base already provides: CreatedBy, CreatedAt, UpdatedAt, Status, UserHostAdd,
    // RewriteUrl (used here as the URL slug), CreatedOnString, EditedOnString.
    public class Blogs : Base
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string Type { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(250)]
        public string Title { get; set; }

        [StringLength(250)]
        public string Heading { get; set; }

        [StringLength(500)]
        [Display(Name = "Sub Heading")]
        public string SubHeading { get; set; }

        [AllowHtml]
        public string Description { get; set; }

        // File name of the cover image stored in /Content/Uploads/
        public string CoverImage { get; set; }

        [Display(Name = "Published On")]
        [DataType(DataType.Date)]
        public DateTime? PublishedOn { get; set; }

        public int DisplayOrder { get; set; }
    }
}
