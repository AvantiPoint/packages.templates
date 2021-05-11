using System.ComponentModel.DataAnnotations;

namespace NuGetFeedTemplate.Data.Models
{
    public class PackageGroupSyndication
    {
        [MaxLength(30)]
        public string PackageGroupName { get; set; }

        [MaxLength(30)]
        public string PublishTargetName { get; set; }

        public PackageGroup PackageGroup { get; set; }

        public PublishTarget PublishTarget { get; set; }
    }
}
