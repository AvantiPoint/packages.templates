using System;
using System.Threading.Tasks;
using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using NuGetFeedTemplate.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NuGetFeedTemplate.Services
{

    public class EmailService : IEmailService
    {
        private ISendGridClient _client { get; }
        private ILogger _logger { get; }
        private EmailAddress From { get; }
        private ITemplateResourceProvider _templateProvider { get; }

        public EmailService(
            ISendGridClient client,
            EmailSettings settings,
            ITemplateResourceProvider templateProvider,
            ILogger<EmailService> logger)
        {
            _client = client;
            _logger = logger;
            _templateProvider = templateProvider;
            From = new EmailAddress(settings.FromAddress, settings.FromName);
        }

        public async Task<bool> SendEmail<T>(string templateName, EmailAddress to, string subject, T context)
        {
            try
            {
                string htmlTemplate = _templateProvider.ReadFile(templateName);
                Handlebars.RegisterHelper("Message", RawOutput);
                var template = Handlebars.Compile(htmlTemplate);
                var msg = MailHelper.CreateSingleEmail(From, to, subject, null, template(context));
                var response = await _client.SendEmailAsync(msg);
                return response.StatusCode == System.Net.HttpStatusCode.Accepted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred while processing an email:\nTemplate: {templateName}\nTo: {to.Email}\nSubject: {subject}");
                return false;
            }
        }

        private void RawOutput(EncodedTextWriter output, Context context, Arguments arguments)
        {
            output.WriteSafeString($"{context["Message"]}");
        }
    }
}
