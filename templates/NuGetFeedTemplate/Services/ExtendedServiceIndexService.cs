using AvantiPoint.Packages.Core;
using AvantiPoint.Packages.Protocol.Models;
using Microsoft.Extensions.Options;
using NuGetFeedTemplate.Configuration;

namespace NuGetFeedTemplate.Services
{
    /// <summary>
    /// Extended service index that adds vulnerability support
    /// </summary>
    public class ExtendedServiceIndexService : IServiceIndexService
    {
        private readonly IServiceIndexService _baseService;
        private readonly IUrlGenerator _urlGenerator;
        private readonly VulnerabilityOptions _vulnerabilityOptions;

        public ExtendedServiceIndexService(
            IServiceIndexService baseService,
            IUrlGenerator urlGenerator,
            IOptions<VulnerabilityOptions> vulnerabilityOptions)
        {
            _baseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
            _urlGenerator = urlGenerator ?? throw new ArgumentNullException(nameof(urlGenerator));
            _vulnerabilityOptions = vulnerabilityOptions?.Value ?? new VulnerabilityOptions();
        }

        public async Task<ServiceIndexResponse> GetAsync(CancellationToken cancellationToken = default)
        {
            var response = await _baseService.GetAsync(cancellationToken);

            // Add vulnerability resource if enabled
            if (_vulnerabilityOptions.Enabled)
            {
                var resources = new List<ServiceIndexItem>(response.Resources);
                
                var baseUrl = !string.IsNullOrEmpty(_vulnerabilityOptions.BaseUrl)
                    ? _vulnerabilityOptions.BaseUrl.TrimEnd('/')
                    : _urlGenerator.GetPackagePublishResourceUrl().TrimEnd('/').Replace("/v2/package", "");

                resources.Add(new ServiceIndexItem
                {
                    ResourceUrl = $"{baseUrl}/v3/vulnerabilities/index.json",
                    Type = "VulnerabilityInfo/6.7.0",
                    Comment = "NuGet vulnerability information"
                });

                response = new ServiceIndexResponse
                {
                    Version = response.Version,
                    Resources = resources
                };
            }

            return response;
        }
    }
}
