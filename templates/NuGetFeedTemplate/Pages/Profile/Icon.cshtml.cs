using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NuGetFeedTemplate.Services;

namespace NuGetFeedTemplate.Pages.Profile
{
    public class IconModel : PageModel
    {
        private readonly IGraphProfilePhotoService _graphProfilePhotoService;
        private readonly ILogger<IconModel> _logger;

        public IconModel(IGraphProfilePhotoService graphProfilePhotoService, ILogger<IconModel> logger)
        {
            _graphProfilePhotoService = graphProfilePhotoService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string email, int size = 50)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Redirect("/img/user.svg");
            }

            try
            {
                Stream photoStream;

                if (string.IsNullOrEmpty(email))
                {
                    // Get current user's photo
                    photoStream = await _graphProfilePhotoService.GetCurrentUserPhotoAsync();
                }
                else
                {
                    // Get specific user's photo by email
                    photoStream = await _graphProfilePhotoService.GetUserPhotoAsync(email);
                }

                if (photoStream != null)
                {
                    return File(photoStream, "image/jpeg");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve profile photo from Microsoft Graph");
            }

            // Fallback to default user image
            return Redirect("/img/user.svg");
        }
    }
}
