using System.Threading.Tasks;
using SendGrid.Helpers.Mail;

namespace NuGetFeedTemplate.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmail<T>(string templateName, EmailAddress to, string subject, T context);
    }
}