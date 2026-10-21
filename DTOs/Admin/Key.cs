using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DTOs
{
    public class Key : Base
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Type { get; set; }
        [AllowHtml]
        public string SVG { get; set; }
        public string Description { get; set; }
        public DateTime CreatedOn { get; set; }

        public string GroupName { get; set; }
        public int DisplayOrder { get; set; }
    }
}
