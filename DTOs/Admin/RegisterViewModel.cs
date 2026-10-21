
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace DTOs
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(100)]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; }

        [Required]
        [System.ComponentModel.DataAnnotations.Compare(
            "Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public int? RoleId { get; set; }

        public IEnumerable<SelectListItem> Roles { get; set; }

        // Used for adding a new role
        [StringLength(100)]
        public string NewRoleTitle { get; set; }

        public List<RegisteredUserViewModel> Users { get; set; }

        public List<RoleViewModel> AllRoles { get; set; }

        public string Message { get; set; }
    }

    public class RegisteredUserViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string RoleTitle { get; set; }
        public bool Status { get; set; }
    }

    public class RoleViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool Status { get; set; }
    }
}
