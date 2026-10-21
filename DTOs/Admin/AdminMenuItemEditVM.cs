
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace DTOs
{
    public class AdminMenuItemEditVM
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Menu title is required.")]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(100)]
        public string Icon { get; set; }

        [StringLength(100)]
        public string Controller { get; set; }

        [StringLength(100)]
        public string Action { get; set; }

        public string Url { get; set; }

        public Guid? GroupId { get; set; }

        public Guid? ParentMenuItemId { get; set; }

        public int DisplayOrder { get; set; }

        public bool Status { get; set; } = true;

        public IEnumerable<SelectListItem> Groups { get; set; }
            = new List<SelectListItem>();

        public IEnumerable<SelectListItem> ParentItems { get; set; }
            = new List<SelectListItem>();
    }
}
