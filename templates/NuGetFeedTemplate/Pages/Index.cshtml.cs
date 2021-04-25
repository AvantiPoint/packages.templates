using System.Threading.Tasks;
using AvantiPoint.Packages.Core;
using AvantiPoint.Packages.Protocol.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NuGetFeedTemplate.Pages
{
    public class IndexModel : PageModel
    {
        private ISearchService _searchService { get; }

        public IndexModel(ISearchService searchService)
        {
            _searchService = searchService;
        }

        public SearchRequest Search { get; set; }

        public SearchResponse SearchResponse { get; set; }

        public int CurrentPage { get; set; }
        public bool HasNext { get; set; }

        public async Task OnGet(
            [FromQuery(Name = "q")] string query = null,
            [FromQuery] int page = 0,
            [FromQuery] bool prerelease = true,
            [FromQuery] string semVerLevel = null,

            // These are unofficial parameters
            [FromQuery] string packageType = null,
            [FromQuery] string framework = null)
        {
            if(!User.Identity.IsAuthenticated)
            {
                return;
            }

            CurrentPage = page;
            Search = new SearchRequest
            {
                Skip = page == 0 ? 0 : page * 20,
                Take = 20,
                IncludePrerelease = prerelease,
                IncludeSemVer2 = semVerLevel == "2.0.0",
                PackageType = packageType,
                Framework = framework,
                Query = query ?? string.Empty,
            };

            SearchResponse =  await _searchService.SearchAsync(Search, default);
            HasNext = (page + 1) * 20 <= SearchResponse.TotalHits;
        }
    }
}
