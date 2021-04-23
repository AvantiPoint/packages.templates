using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NuGetFeedTemplate.Pages
{
    public class ProfileModel : PageModel
    {
        private const string requestUriFormat = "https://www.gravatar.com/avatar/{0}?s={1}&d={2}";
        public IActionResult OnGet()
        {
            if(User.Identity.IsAuthenticated)
            {
                var email = User.Identity.Name;
                return Redirect(GetGravatarUri(email));
            }

            return Redirect("/img/user.svg");
        }

        private string GetGravatarUri(string email)
            => string.Format(requestUriFormat, GetMd5Hash(email), 50, "mp");

        static string GetMd5Hash(string str)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(str));

            var sBuilder = new StringBuilder();

            if (hash != null)
            {
                for (var i = 0; i < hash.Length; i++)
                    sBuilder.Append(hash[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }
    }
}
