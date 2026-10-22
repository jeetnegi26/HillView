using System;
using System.Collections.Generic;

namespace DTOs
{
    public class UISectionVM
    {
        public Guid Id { get; set; }
        public int DisplayOrder { get; set; }
        public string Title { get; set; }
        public string Heading { get; set; }
        public string SubHeading { get; set; }
        public string Description { get; set; }
        public string BackgroundColor { get; set; }
        public string CssClass { get; set; }
        public string SettingsJson { get; set; }

        public List<ImageVM> Images { get; set; } = new List<ImageVM>();
        public List<ButtonVM> Buttons { get; set; } = new List<ButtonVM>();
        public List<UISectionItemVM> Items { get; set; }
            = new List<UISectionItemVM>();
    }
}