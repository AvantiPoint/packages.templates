using System.Net.Mail;

namespace NuGetFeedTemplate.Services;

public interface IEmailService
{
    Task<bool> SendEmail<T>(string templateName, MailAddress to, string subject, T context);
}
