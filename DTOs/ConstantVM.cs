using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class ConstantVM
    {
        /*Imp to change befor upload project */
        public const string HostedTimeZone = "Indian Standard Time";
        //for session and cookie
        public const string WebUserCookie = "HillViewWebsiteSessionId";
        public const string VisitCookie = "HillViewWebsiteCookie";
        //web name change for each website

        public static class Role
        {
            public const string SuperAdmin = "SuperAdmin";
            public const string Admin = "Admin";
            public const string User = "User";
        }
        public enum Status
        {
            Pending = 0,
            Processing = 1,
            Completed = 2,
        }
        public enum LoginResult
        {
            Success,
            LockedOut,
            RequiresVerification,
            Failure,
            ExternalAuthorize
        }
        public enum TokenType
        {
            EmailConfirmation,
            ResetPassword
        }
        public enum ProviderType
        {
            Web,
            Facebook,
            Google
        }
        public static class Message
        {
            public const string Error = "Something went wrong!";
            public const string Add = "{0}, is succesfully added!";
            public const string Edit = "{0}, is succesfully edited!";
            public const string Delete = "{0}, is succesfully deleted!";
            public const string Exist = "{0}, is already exist!";
        }

        public enum ImageType
        {
            Portrait = 0,
            Landscape = 1,
        }
    }
    public enum BannerType
    {
        Home,
        Project,
        Brand,
        About,
        Contact
    }
}
