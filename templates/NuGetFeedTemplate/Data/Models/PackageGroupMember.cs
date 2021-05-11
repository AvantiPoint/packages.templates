using System.ComponentModel.DataAnnotations;

namespace NuGetFeedTemplate.Data.Models
{
    public class PackageGroupMember
    {
        [MaxLength(30)]
        public string PackageGroupName { get; set; }

        public string PackageId { get; set; }

        public PackageGroup PackageGroup { get; set; }
    }
}
