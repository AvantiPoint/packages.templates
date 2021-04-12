using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NuGetFeedTemplate.Data.Models
{
    public class PackageDownload
    {
        public Guid Id { get; set; }
        public DateTimeOffset Downloaded { get; set; }
        public string AuthTokenKey { get; set; }
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
        public string PackageId { get; set; }
        public string PackageVersion { get; set; }

        public AuthToken AuthToken { get; set; }
    }
}
