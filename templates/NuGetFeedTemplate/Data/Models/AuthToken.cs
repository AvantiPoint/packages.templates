using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace NuGetFeedTemplate.Data.Models
{
    public class AuthToken
    {
        private const int TokenLength = 32;

        public AuthToken()
        {
             Key = GenerateKey(TokenLength);
        }

        [MaxLength(TokenLength)]
        public string Key { get; set; }

        [MaxLength(60)]
        public string Description { get; set; }

        public string UserEmail { get; set; }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset Expires { get; set; }

        public bool Revoked { get; set; }

        public User User { get; set; }

        public bool IsValid()
        {
            if (Revoked || DateTimeOffset.Now > Expires)
                return false;

            return true;
        }

        private static string GenerateKey(int length)
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(length);
            var key = Convert.ToBase64String(tokenBytes);
            key = Regex.Replace(key, @"[=]+$", string.Empty);
            return key[..length];
        }
    }
}
