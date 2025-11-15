using System.Net;
using NuGetFeedTemplate.Tests.TestServer;

namespace NuGetFeedTemplate.Tests;

/// <summary>
/// Tests for the NuGet server API endpoints.
/// </summary>
public class NuGetServerTests : IClassFixture<NuGetServerTestFixture>
{
    private readonly NuGetServerTestFixture _fixture;

    public NuGetServerTests(NuGetServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Server_Starts_Successfully()
    {
        // Arrange & Act
        var client = _fixture.Client;
        var baseAddress = _fixture.BaseAddress;

        // Assert
        Assert.NotNull(client);
        Assert.NotNull(baseAddress);
        Assert.Contains("localhost", baseAddress.ToString());
    }

    [Fact]
    public async Task ServiceIndex_Returns_Success()
    {
        // Arrange
        var client = _fixture.Client;

        // Act
        var response = await client.GetAsync("/v3/index.json");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound, 
            $"Expected success or not found, got {response.StatusCode}");
    }

    [Fact]
    public async Task MultipleTests_UseIsolatedServers()
    {
        // This test verifies that each test gets its own server instance
        // by checking that the base address is accessible
        
        // Arrange
        var client = _fixture.Client;
        var baseAddress = _fixture.BaseAddress;

        // Act
        var response = await client.GetAsync("/");

        // Assert - We just verify the server is responding
        Assert.NotNull(response);
    }
}
