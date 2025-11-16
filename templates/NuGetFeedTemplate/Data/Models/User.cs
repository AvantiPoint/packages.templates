using System;
using System.Collections.Generic;

namespace NuGetFeedTemplate.Data.Models
{
    public class User
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public bool PackagePublisher { get; set; }
        public bool IsRevoked { get; set; }

        public List<AuthToken> Tokens { get; set; }
    }
}
