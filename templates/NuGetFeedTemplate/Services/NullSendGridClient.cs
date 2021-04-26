using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NuGetFeedTemplate.Services
{
    public class NullSendGridClient : ISendGridClient
    {
        public string UrlPath { get; set; }
        public string Version { get; set; }
        public string MediaType { get; set; }

        public AuthenticationHeaderValue AddAuthorization(KeyValuePair<string, string> header)
        {
            throw new NotImplementedException();
        }

        public Task<Response> MakeRequest(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Response> RequestAsync(BaseClient.Method method, string requestBody = null, string queryParams = null, string urlPath = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Response> SendEmailAsync(SendGridMessage msg, CancellationToken cancellationToken = default)
        {
            var response = new Response(HttpStatusCode.Accepted,
                new StringContent(string.Empty),
                null);
            return Task.FromResult(response);
        }
    }
}
