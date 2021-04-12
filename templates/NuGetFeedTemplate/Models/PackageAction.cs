using System;

namespace NuGetFeedTemplate.Models
{
    public class PackageAction
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string TokenDescription { get; set; }
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
        public string Timestamp => DateTimeOffset.Now.ToString("F");
    }
}
