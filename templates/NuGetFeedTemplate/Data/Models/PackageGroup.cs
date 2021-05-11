using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NuGetFeedTemplate.Data.Models
{
    public class PackageGroup
    {
        [MaxLength(30)]
        public string Name { get; set; }

        public List<PackageGroupMember> Members { get; set; }

        public List<PackageGroupSyndication> Syndications { get; set; }
    }
}
