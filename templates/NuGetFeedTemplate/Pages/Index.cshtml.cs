using System;
using System.Collections.Generic;
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

        // Properties for UI binding
        public string SelectedRuntime { get; set; }
        public string SelectedFramework { get; set; }
        public string SelectedPackageType { get; set; }

        // Helper method to build query string with all current filters
        public string BuildQueryString(int? page = null, string query = null)
        {
            var parameters = new List<string>();
            
            if (page.HasValue)
                parameters.Add($"page={page.Value}");
            
            var searchQuery = query ?? Search?.Query;
            if (!string.IsNullOrEmpty(searchQuery))
                parameters.Add($"q={Uri.EscapeDataString(searchQuery)}");
            
            parameters.Add($"prerelease={Search?.IncludePrerelease.ToString().ToLower() ?? "true"}");
            
            if (!string.IsNullOrEmpty(SelectedFramework))
                parameters.Add($"framework={Uri.EscapeDataString(SelectedFramework)}");
            
            if (!string.IsNullOrEmpty(SelectedRuntime))
                parameters.Add($"runtime={Uri.EscapeDataString(SelectedRuntime)}");
            
            if (!string.IsNullOrEmpty(SelectedPackageType))
                parameters.Add($"packageType={Uri.EscapeDataString(SelectedPackageType)}");
            
            return parameters.Count > 0 ? "?" + string.Join("&", parameters) : "";
        }

        public async Task OnGet(
            [FromQuery(Name = "q")] string query = null,
            [FromQuery] int page = 0,
            [FromQuery] bool prerelease = true,
            [FromQuery] string semVerLevel = null,

            // These are unofficial parameters
            [FromQuery] string packageType = null,
            [FromQuery] string framework = null,
            [FromQuery] string runtime = null)
        {
            if(!User.Identity.IsAuthenticated)
            {
                return;
            }

            CurrentPage = page;
            
            // Store selected values for UI
            SelectedRuntime = runtime;
            SelectedFramework = framework;
            SelectedPackageType = packageType;

            // Handle runtime-specific framework filtering
            var effectiveFramework = framework;
            if (!string.IsNullOrEmpty(runtime))
            {
                // Runtime filter requires packages with platform-specific targets
                if (!string.IsNullOrEmpty(framework))
                {
                    // Combine framework with runtime: e.g., "net8.0" + "android" -> "net8.0-android"
                    effectiveFramework = $"{framework}-{runtime.ToLower()}";
                }
                else
                {
                    // Default to .NET 8.0 (LTS) when only runtime is selected
                    // This ensures we get platform-specific packages
                    effectiveFramework = $"net8.0-{runtime.ToLower()}";
                }
            }

            Search = new SearchRequest
            {
                Skip = page == 0 ? 0 : page * 20,
                Take = 20,
                IncludePrerelease = prerelease,
                IncludeSemVer2 = semVerLevel == "2.0.0",
                PackageType = packageType,
                Framework = effectiveFramework,
                Query = query ?? string.Empty,
            };

            SearchResponse =  await _searchService.SearchAsync(Search, default);
            HasNext = (page + 1) * 20 <= SearchResponse.TotalHits;
        }
    }
}
