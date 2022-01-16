using System;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGetFeedTemplate.Configuration;
using PostmarkDotNet;

namespace NuGetFeedTemplate.Services
{
    public class PostmarkEmailService : BaseEmailService
    {
        private PostmarkClient _client { get; }

        public PostmarkEmailService(
            PostmarkClient client,
            EmailSettings settings,
            ITemplateResourceProvider templateProvider,
            ILogger<PostmarkEmailService> logger)
            : base(settings, templateProvider, logger)
        {
            _client = client;
        }

        protected override async Task<bool> SendInternal(MailMessage message)
        {
            var html = message.IsBodyHtml ? message.Body : null;
            var plainText = message.IsBodyHtml ? null : message.Body;
            var response = await _client.SendMessageAsync(message.From.ToString(), message.To.First().ToString(), message.Subject, plainText, html);

            if (response.Status != PostmarkStatus.Success)
            {
                Logger.LogWarning($"Postmark responded with an unexpected response code {response.Status} - Error Code: {response.ErrorCode} - Message: {response.Message}");
                throw new Exception($"Postmark responded with an unexpected response code {response.Status} - Error Code: {response.ErrorCode} - Message: {response.Message}");
            }

            return true;
        }
    }
}
