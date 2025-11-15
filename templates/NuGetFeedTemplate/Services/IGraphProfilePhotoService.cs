namespace NuGetFeedTemplate.Services;

public interface IGraphProfilePhotoService
{
    Task<Stream> GetUserPhotoAsync(string email);
    Task<Stream> GetCurrentUserPhotoAsync();
}
