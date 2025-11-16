namespace NuGetFeedTemplate.Models;

public record ExternalAuthCallbackRequest(string Code, string State, string Error);
