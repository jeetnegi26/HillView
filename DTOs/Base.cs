using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class Base
    {
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Status { get; set; }
        public string UserHostAdd { get; set; }
        public string RewriteUrl { get; set; }
        public string CreatedOnString { get; set; }
        public string EditedOnString { get; set; }
    }
}
