using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NuGetFeedTemplate.Services;

namespace NuGetFeedTemplate.Pages.Profile
{
    [AllowAnonymous]
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
                    // Get current user's email from claims
                    email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity.Name;
                }

                // Get user's photo by email
                photoStream = await _graphProfilePhotoService.GetUserPhotoAsync(email);

                if (photoStream != null)
                {
                    return File(photoStream, "image/jpeg");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve profile photo");
            }

            // Fallback to default user image
            return Redirect("/img/user.svg");
        }
    }
}
