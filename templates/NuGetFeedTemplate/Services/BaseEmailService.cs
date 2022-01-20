using System.Net.Mail;
using HandlebarsDotNet;
using NuGetFeedTemplate.Configuration;

namespace NuGetFeedTemplate.Services;

public abstract class BaseEmailService : IEmailService
{
    private ITemplateResourceProvider _templateProvider { get; }

    protected BaseEmailService(EmailSettings settings,
        ITemplateResourceProvider templateProvider,
        ILogger logger)
    {
        Logger = logger;
        _templateProvider = templateProvider;
        From = new MailAddress(settings.FromAddress, settings.FromName);
    }

    protected MailAddress From { get; }
    protected ILogger Logger { get; }

    public async Task<bool> SendEmail<T>(string templateName, MailAddress to, string subject, T context)
    {
        string htmlTemplate = _templateProvider.ReadFile(templateName);
        Handlebars.RegisterHelper("Message", RawOutput);
        var template = Handlebars.Compile(htmlTemplate);

        try
        {
            using var message = new MailMessage(From, to)
            {
                Body = template(context),
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.Default,
                Subject = subject
            };
            return await SendInternal(message);
        }
        catch (System.Exception ex)
        {
            Logger.LogError(ex, $"An unexpected error occurred while processing an email:\nTemplate: {templateName}\nTo: {to}\nSubject: {subject}");
            return false;
        }
    }

    protected abstract Task<bool> SendInternal(MailMessage message);

    private void RawOutput(EncodedTextWriter output, Context context, Arguments arguments)
    {
        output.WriteSafeString($"{context["Message"]}");
    }
}
