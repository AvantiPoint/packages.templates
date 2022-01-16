using System.Net.Mail;
using System.Threading.Tasks;

namespace NuGetFeedTemplate.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmail<T>(string templateName, MailAddress to, string subject, T context);
    }
}