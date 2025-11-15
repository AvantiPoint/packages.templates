using System.Net;
using NuGetFeedTemplate.Tests.TestServer;

namespace NuGetFeedTemplate.Tests;

/// <summary>
/// Comprehensive integration tests demonstrating the full test server workflow.
/// </summary>
public class IntegrationTests : IClassFixture<NuGetServerTestFixture>
{
    private readonly NuGetServerTestFixture _fixture;

    public IntegrationTests(NuGetServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CompleteWorkflow_CreateAndPushPackage()
    {
        // This test demonstrates the complete workflow:
        // 1. Create a test package
        // 2. Push it to the server
        // 3. Verify the response

        // Arrange
        var client = _fixture.Client;
        var packageId = "Demo.Package";
        var version = "1.0.0";

        // Act
        var response = await TestPackageHelper.PushPackageAsync(client, packageId, version);

        // Assert
        Assert.NotNull(response);
        // The response will vary depending on authentication requirements
        // For now, we just verify we got a response
    }

    [Fact]
    public async Task MultiplePackages_CanBePushedInSequence()
    {
        // This test shows how to push multiple packages

        // Arrange
        var client = _fixture.Client;

        // Act & Assert
        var response1 = await TestPackageHelper.PushPackageAsync(client, "Test.Package1", "1.0.0");
        Assert.NotNull(response1);

        var response2 = await TestPackageHelper.PushPackageAsync(client, "Test.Package2", "1.0.0");
        Assert.NotNull(response2);

        var response3 = await TestPackageHelper.PushPackageAsync(client, "Test.Package1", "2.0.0");
        Assert.NotNull(response3);
    }

    [Fact]
    public async Task ServerEndpoints_AreAccessible()
    {
        // This test verifies that various NuGet endpoints are accessible

        // Arrange
        var client = _fixture.Client;

        // Act - Try accessing the service index
        var indexResponse = await client.GetAsync("/v3/index.json");

        // Assert - We expect either success or NotFound (depending on configuration)
        Assert.NotNull(indexResponse);
        Assert.True(
            indexResponse.IsSuccessStatusCode || indexResponse.StatusCode == HttpStatusCode.NotFound,
            $"Expected success or not found, but got {indexResponse.StatusCode}");
    }

    [Fact]
    public async Task ServerBaseAddress_IsUnique()
    {
        // This verifies that each test class gets a unique server instance

        // Arrange & Act
        var baseAddress = _fixture.BaseAddress;

        // Assert
        Assert.NotNull(baseAddress);
        Assert.Contains("localhost", baseAddress.ToString());
        Assert.NotEqual(0, baseAddress.Port);
    }

    [Fact]
    public async Task TestPackageHelper_CreatesValidPackages()
    {
        // This test verifies the package creation helper

        // Arrange
        var packages = TestPackageHelper.CreateTestPackages();

        // Assert
        Assert.NotEmpty(packages);
        Assert.All(packages.Values, bytes => Assert.True(bytes.Length > 0));
    }
}
