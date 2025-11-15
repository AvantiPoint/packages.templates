using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace NuGetFeedTemplate.Services;

public class GraphProfilePhotoService : IGraphProfilePhotoService
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger<GraphProfilePhotoService> _logger;

    public GraphProfilePhotoService(GraphServiceClient graphServiceClient, ILogger<GraphProfilePhotoService> logger)
    {
        _graphServiceClient = graphServiceClient;
        _logger = logger;
    }

    public async Task<Stream> GetCurrentUserPhotoAsync()
    {
        try
        {
            var photoStream = await _graphServiceClient.Me.Photo.Content.GetAsync();
            return photoStream;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve current user photo from Microsoft Graph");
            return null;
        }
    }

    public async Task<Stream> GetUserPhotoAsync(string email)
    {
        try
        {
            // Try to find the user by email
            var users = await _graphServiceClient.Users.GetAsync(requestConfig =>
            {
                requestConfig.QueryParameters.Filter = $"mail eq '{email}' or userPrincipalName eq '{email}'";
                requestConfig.QueryParameters.Select = new[] { "id" };
            });

            if (users?.Value?.Count > 0)
            {
                var userId = users.Value[0].Id;
                var photoStream = await _graphServiceClient.Users[userId].Photo.Content.GetAsync();
                return photoStream;
            }

            _logger.LogWarning("User not found in Microsoft Graph: {Email}", email);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve user photo from Microsoft Graph for {Email}", email);
            return null;
        }
    }
}
