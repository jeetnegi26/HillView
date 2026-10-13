
using Microsoft.AspNet.Identity;

namespace Services
{
    public static class PasswordHashHelper
    {
        private static readonly PasswordHasher Hasher =
            new PasswordHasher();

        public static string HashPassword(string password)
        {
            return Hasher.HashPassword(password);
        }

        public static bool VerifyPassword(
            string hashedPassword,
            string submittedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            var result = Hasher.VerifyHashedPassword(
                hashedPassword,
                submittedPassword);

            return result != PasswordVerificationResult.Failed;
        }
    }
}
