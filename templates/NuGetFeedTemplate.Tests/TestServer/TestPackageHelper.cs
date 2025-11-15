using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace NuGetFeedTemplate.Tests.TestServer;

/// <summary>
/// Helper class for creating and seeding test packages.
/// </summary>
public static class TestPackageHelper
{
    /// <summary>
    /// Creates a simple test NuGet package (.nupkg) in memory.
    /// </summary>
    /// <param name="id">Package ID.</param>
    /// <param name="version">Package version.</param>
    /// <param name="description">Package description.</param>
    /// <returns>Package content as a byte array.</returns>
    public static byte[] CreatePackage(string id, string version, string description = "Test package")
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Create the .nuspec file
            var nuspecEntry = archive.CreateEntry($"{id}.nuspec");
            using (var nuspecStream = nuspecEntry.Open())
            {
                var nuspec = CreateNuspec(id, version, description);
                var bytes = Encoding.UTF8.GetBytes(nuspec);
                nuspecStream.Write(bytes, 0, bytes.Length);
            }

            // Create a dummy content file
            var contentEntry = archive.CreateEntry("content/readme.txt");
            using (var contentStream = contentEntry.Open())
            {
                var content = Encoding.UTF8.GetBytes($"This is {id} version {version}");
                contentStream.Write(content, 0, content.Length);
            }

            // Create [Content_Types].xml
            var contentTypesEntry = archive.CreateEntry("[Content_Types].xml");
            using (var contentTypesStream = contentTypesEntry.Open())
            {
                var contentTypes = CreateContentTypes();
                var bytes = Encoding.UTF8.GetBytes(contentTypes);
                contentTypesStream.Write(bytes, 0, bytes.Length);
            }
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Seeds a package to the test server using HTTP push.
    /// </summary>
    /// <param name="client">HTTP client connected to the test server.</param>
    /// <param name="packageId">Package ID.</param>
    /// <param name="version">Package version.</param>
    /// <param name="apiKey">Optional API key for authentication.</param>
    /// <returns>HTTP response from the push operation.</returns>
    public static async Task<HttpResponseMessage> PushPackageAsync(
        HttpClient client, 
        string packageId, 
        string version,
        string? apiKey = null)
    {
        var packageBytes = CreatePackage(packageId, version);
        
        using var content = new ByteArrayContent(packageBytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/v2/package");
        request.Content = content;
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Add("X-NuGet-ApiKey", apiKey);
        }

        return await client.SendAsync(request);
    }

    /// <summary>
    /// Creates a set of common test packages.
    /// </summary>
    /// <returns>Dictionary of package ID and version to package bytes.</returns>
    public static Dictionary<string, byte[]> CreateTestPackages()
    {
        return new Dictionary<string, byte[]>
        {
            ["Test.PackageA-1.0.0"] = CreatePackage("Test.PackageA", "1.0.0", "Test Package A version 1.0.0"),
            ["Test.PackageA-1.1.0"] = CreatePackage("Test.PackageA", "1.1.0", "Test Package A version 1.1.0"),
            ["Test.PackageB-2.0.0"] = CreatePackage("Test.PackageB", "2.0.0", "Test Package B version 2.0.0"),
        };
    }

    private static string CreateNuspec(string id, string version, string description)
    {
        var nuspec = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(XName.Get("package", "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"),
                new XElement("metadata",
                    new XElement("id", id),
                    new XElement("version", version),
                    new XElement("authors", "Test Author"),
                    new XElement("description", description),
                    new XElement("requireLicenseAcceptance", "false")
                )
            )
        );

        return nuspec.ToString();
    }

    private static string CreateContentTypes()
    {
        XNamespace ns = "http://schemas.openxmlformats.org/package/2006/content-types";
        var contentTypes = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "Types",
                new XElement(ns + "Default",
                    new XAttribute("Extension", "nuspec"),
                    new XAttribute("ContentType", "application/octet-stream")
                ),
                new XElement(ns + "Default",
                    new XAttribute("Extension", "txt"),
                    new XAttribute("ContentType", "text/plain")
                )
            )
        );

        return contentTypes.ToString();
    }
}
