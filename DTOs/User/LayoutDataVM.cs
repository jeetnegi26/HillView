using System.Collections.Generic;

namespace DTOs
{
    public class LayoutDataVM
    {
        // Brand information
        public string BrandName { get; set; }
        public string Logo { get; set; }
        public string Phone { get; set; }
        public string TollFree { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string BusinessHours { get; set; }
        public string Copyright { get; set; }

        // Header navigation
        public List<LayoutNavVM> Navigation { get; set; }
            = new List<LayoutNavVM>();

        // Footer link groups
        public List<LayoutFooterLinkVM> FooterLinks { get; set; }
            = new List<LayoutFooterLinkVM>();

        // Social media links
        public List<LayoutSocialLinkVM> SocialLinks { get; set; }
            = new List<LayoutSocialLinkVM>();
    }

    public class LayoutNavVM
    {
        public string Title { get; set; }
        public string Slug { get; set; }
    }

    public class LayoutFooterLinkVM
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string GroupName { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class LayoutSocialLinkVM
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public int DisplayOrder { get; set; }
    }
}