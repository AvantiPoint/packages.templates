using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NuGetFeedTemplate.Data.Models
{
    public class AuthToken
    {
        public AuthToken()
        {
             Key = Guid.NewGuid().ToString().Replace("-", string.Empty);
        }

        [MaxLength(32)]
        public string Key { get; set; }

        [MaxLength(60)]
        public string Description { get; set; }

        public string UserEmail { get; set; }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset Expires { get; set; }

        public bool Revoked { get; set; }

        public User User { get; set; }

        public List<PackageDownload> Downloads { get; set; }

        public bool IsValid()
        {
            if (Revoked || DateTimeOffset.Now > Expires)
                return false;

            return true;
        }
    }
}
