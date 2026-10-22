using System;
using System.Collections.Generic;

namespace DTOs
{
    // Everything the home page (or any CMS page) needs to render.
    public class UIPageVM
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string CanonicalUrl { get; set; }

        public List<UISectionVM> Sections { get; set; }
            = new List<UISectionVM>();

        public List<TestimonialVM> Testimonials { get; set; }
            = new List<TestimonialVM>();
    }
}