using System.Net.Mail;

namespace NuGetFeedTemplate.Services;

public class NullEmailService : IEmailService
{
    public Task<bool> SendEmail<T>(string templateName, MailAddress to, string subject, T context) =>
        Task.FromResult(true);
}
