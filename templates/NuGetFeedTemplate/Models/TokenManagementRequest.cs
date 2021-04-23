namespace NuGetFeedTemplate.Models
{
    public class TokenManagementRequest
    {
        public TokenRequestType Type { get; set; }

        public string Description { get; set; }

        public string Id { get; set; }
    }
}
