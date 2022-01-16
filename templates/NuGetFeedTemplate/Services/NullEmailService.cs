using System.Net.Mail;
using System.Threading.Tasks;

namespace NuGetFeedTemplate.Services
{
    public class NullEmailService : IEmailService
    {
        public Task<bool> SendEmail<T>(string templateName, MailAddress to, string subject, T context) =>
            Task.FromResult(true);
    }
}
