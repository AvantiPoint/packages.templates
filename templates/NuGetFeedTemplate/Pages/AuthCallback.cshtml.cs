using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NuGetFeedTemplate.Pages;

[AllowAnonymous]
public class AuthCallbackModel : PageModel
{
    public void OnGet()
    {
    }
}
