using System;
using System.Collections.Generic;

namespace DTOs
{
    public class AdminMenuGroupVM
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public int DisplayOrder { get; set; }

        public bool Status { get; set; }

        public List<AdminMenuItemVM> Items { get; set; }
            = new List<AdminMenuItemVM>();
        public List<AdminMenuItemVM> StandaloneItems { get; set; }
= new List<AdminMenuItemVM>();
    }

    public class AdminMenuItemVM
    {
        public Guid Id { get; set; }

        public Guid? GroupId { get; set; }

        public Guid? ParentMenuItemId { get; set; }

        public string Title { get; set; }

        public string Icon { get; set; }

        public string Controller { get; set; }

        public string Action { get; set; }

        public string Url { get; set; }

        public int DisplayOrder { get; set; }

        public bool Status { get; set; }

        public List<AdminMenuItemVM> Children { get; set; }
            = new List<AdminMenuItemVM>();
    }


    public class AdminSidebarVM
    {
        public List<AdminMenuItemVM> StandaloneItems { get; set; }
            = new List<AdminMenuItemVM>();

        public List<AdminMenuGroupVM> Groups { get; set; }
            = new List<AdminMenuGroupVM>();
    }

}
