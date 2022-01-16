namespace NuGetFeedTemplate.Configuration
{
    public class EmailSettings
    {
        public string FromAddress { get; set; }
        public string FromName { get; set; }
        public string SendGridKey { get; set; }
        public string PostmarkKey { get; set; }
        public string TemplatesDirectory { get; set; }
    }
}
