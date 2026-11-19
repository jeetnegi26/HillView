using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class CaseStudyDetailVM
    {
        public Blogs CurrentStudy { get; set; }
        public List<Blogs> RecentStudies { get; set; }
    }
}
