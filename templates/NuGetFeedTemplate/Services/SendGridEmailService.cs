using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGetFeedTemplate.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NuGetFeedTemplate.Services
{

    public class SendGridEmailService : BaseEmailService
    {
        private ISendGridClient _client { get; }

        public SendGridEmailService(
            ISendGridClient client,
            EmailSettings settings,
            ITemplateResourceProvider templateProvider,
            ILogger<SendGridEmailService> logger)
            :base(settings, templateProvider, logger)
        {
            _client = client;
        }

        protected override async Task<bool> SendInternal(MailMessage message)
        {
            var from = new EmailAddress(message.From.Address, message.From.DisplayName);
            var to = new EmailAddress(message.To.First().Address, message.To.First().DisplayName);
            var html = message.IsBodyHtml ? message.Body : null;
            var plainText = message.IsBodyHtml ? null : message.Body;

            var msg = MailHelper.CreateSingleEmail(from, to, message.Subject, plainText, html);
            var response = await _client.SendEmailAsync(msg);
            return response.StatusCode == System.Net.HttpStatusCode.Accepted;
        }
    }
}
