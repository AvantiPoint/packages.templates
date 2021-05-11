using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace NuGetFeedTemplate.Data.Models
{
    public class PublishTarget : IEqualityComparer<PublishTarget>
    {
        [MaxLength(30)]
        public string Name { get; set; }
        public Uri PublishEndpoint { get; set; }
        [MaxLength(100)]
        public string ApiToken { get; set; }
        public bool Legacy { get; set; }
        public string AddedBy { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public List<PackageGroupSyndication> PackageGroups { get; set; }

        public bool Equals(PublishTarget x, PublishTarget y)
        {
            return x.PublishEndpoint == y.PublishEndpoint;
        }

        public int GetHashCode([DisallowNull] PublishTarget obj)
        {
            return obj.PublishEndpoint.GetHashCode();
        }
    }
}
