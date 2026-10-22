using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class ContactVM
    {
        [Required(ErrorMessage = "Please enter your full name.")]
        [StringLength(250, ErrorMessage = "The name is too long.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(250, ErrorMessage = "The email address is too long.")]
        public string Email { get; set; }

        // ContactUs.Phone is varchar(20), so only plain characters are allowed.
        [StringLength(20, ErrorMessage = "The phone number is too long.")]
        [RegularExpression(@"^[0-9+()\-\s]*$",
            ErrorMessage = "Phone can contain only digits, spaces, + ( ) and -.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Please enter your message.")]
        [StringLength(4000, ErrorMessage = "The message is too long (maximum 4000 characters).")]
        public string Message { get; set; }

        // Honeypot field. Real visitors never see it, so it stays empty.
        // Bots fill it in, and the controller then ignores the submission.
        public string Website { get; set; }
    }
}