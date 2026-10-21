
using System;
using System.Collections.Generic;

namespace DTOs
{
    public class HomePageVM
    {
        public Guid PageId { get; set; }

        public string PageTitle { get; set; }

        public List<HomePageSectionVM> Sections { get; set; }
            = new List<HomePageSectionVM>();

        public List<TestimonialVM> Testimonials { get; set; }
            = new List<TestimonialVM>();
    }

    public class HomePageSectionVM
    {
        public Guid PageSectionId { get; set; }

        public Guid SectionId { get; set; }

        public int PageSectionOrder { get; set; }

        public SectionVM Section { get; set; }

        public List<SectionItemVM> Items { get; set; }
            = new List<SectionItemVM>();

        public List<ImageVM> Images { get; set; }
            = new List<ImageVM>();

        public List<ButtonVM> Buttons { get; set; }
            = new List<ButtonVM>();
    }
}
