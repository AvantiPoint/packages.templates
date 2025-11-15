using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace NuGetFeedTemplate.Data.Models
{
    public class AuthToken
    {
        public AuthToken()
        {
             Key = GenerateSecureToken();
        }

        [MaxLength(32)]
        public string Key { get; set; }

        [MaxLength(60)]
        public string Description { get; set; }

        public string UserEmail { get; set; }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset Expires { get; set; }

        public bool Revoked { get; set; }

        public bool IsSystemToken { get; set; }

        public User User { get; set; }

        public bool IsValid()
        {
            if (Revoked || DateTimeOffset.Now > Expires)
                return false;

            return true;
        }

        /// <summary>
        /// Generates a cryptographically secure random token
        /// </summary>
        /// <returns>A 32-character hexadecimal string</returns>
        private static string GenerateSecureToken()
        {
            // Generate 16 random bytes (128 bits) for a secure token
            byte[] tokenBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            
            // Convert to hexadecimal string (32 characters)
            return BitConverter.ToString(tokenBytes).Replace("-", string.Empty).ToLower();
        }
    }
}
