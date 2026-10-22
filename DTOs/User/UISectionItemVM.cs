using System;
using System.Collections.Generic;

namespace DTOs
{
    public class UISectionItemVM
    {
        public Guid Id { get; set; }
        public Guid SectionId { get; set; }
        public Guid? ParentSectionItemId { get; set; }

        public string Title { get; set; }
        public string Heading { get; set; }
        public string SubHeading { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
        public string Label { get; set; }
        public string LinkUrl { get; set; }
        public string AdditionalData { get; set; }

        public List<ImageVM> Images { get; set; } = new List<ImageVM>();
        public List<ButtonVM> Buttons { get; set; } = new List<ButtonVM>();
        public List<UISectionItemVM> Children { get; set; }
            = new List<UISectionItemVM>();
    }
}