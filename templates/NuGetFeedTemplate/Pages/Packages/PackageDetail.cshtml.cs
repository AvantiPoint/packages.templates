using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AvantiPoint.Packages.Core;
using AvantiPoint.Packages.Protocol.Models;

namespace NuGetFeedTemplate.Pages.Packages
{
    public class PackageDetailModel : PageModel
    {
        private IPackageMetadataService _metadataService { get; }

        public PackageDetailModel(IPackageMetadataService metadataService)
        {
            _metadataService = metadataService;
        }

        public PackageInfoCollection PackageInfo { get; set; }

        public string ProjectUrlTitle => GetProjectUrlTitle(PackageInfo);

        public async Task OnGet(string packageId, string packageVersion = null)
        {
            PackageInfo = await _metadataService.GetPackageInfo(packageId, packageVersion);
        }

        private static string GetProjectUrlTitle(PackageInfoCollection packageInfo)
        {
            if (!Uri.TryCreate(packageInfo?.ProjectUrl, UriKind.Absolute, out var uri))
                return "Project Site";

            if (uri.Host == "dev.azure.com")
                return "Azure DevOps";

            return uri.Host;
        }
    }
}
