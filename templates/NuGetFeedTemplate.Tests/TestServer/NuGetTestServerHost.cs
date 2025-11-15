using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AvantiPoint.Packages;
using AvantiPoint.Packages.Core;
using AvantiPoint.Packages.Hosting;
using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Services;

namespace NuGetFeedTemplate.Tests.TestServer;

/// <summary>
/// Provides an in-process NuGet test server host for integration testing.
/// </summary>
public sealed class NuGetTestServerHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly HttpClient _client;

    private NuGetTestServerHost(WebApplication app, Uri baseAddress)
    {
        _app = app;
        BaseAddress = baseAddress;
        _client = new HttpClient { BaseAddress = baseAddress };
    }

    /// <summary>
    /// Gets the base address of the test server.
    /// </summary>
    public Uri BaseAddress { get; }

    /// <summary>
    /// Gets the HTTP client configured to communicate with the test server.
    /// </summary>
    public HttpClient Client => _client;

    /// <summary>
    /// Starts a new instance of the NuGet test server on a random free port.
    /// </summary>
    /// <param name="configureServices">Optional action to configure additional services.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new instance of <see cref="NuGetTestServerHost"/>.</returns>
    public static async Task<NuGetTestServerHost> StartAsync(
        Action<IServiceCollection>? configureServices = null,
        CancellationToken cancellationToken = default)
    {
        // Find a free port
        var port = GetAvailablePort();

        var builder = WebApplication.CreateBuilder();
        
        // Configure to listen on the specific port
        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, port);
        });

        // Configure NuGet package API with test settings
        builder.Services.AddNuGetPackageApi(options =>
        {
            // Use file storage for tests (temp directory)
            options.AddFileStorage();

            // Add feed configuration
            options.AddFeedConfiguration();
            
            // Add test feed services (using in-memory database)
            options.AddTestFeedServices();
        });

        // Disable authentication for tests
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = null;
        });

        builder.Services.AddControllers();

        // Apply custom service configuration
        configureServices?.Invoke(builder.Services);

        var app = builder.Build();

        // Configure the application
        app.UseRouting();
        app.UseOperationCancelledMiddleware();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapNuGetApiRoutes();

        await app.StartAsync(cancellationToken);

        var baseAddress = new Uri($"http://localhost:{port}");

        return new NuGetTestServerHost(app, baseAddress);
    }

    /// <summary>
    /// Disposes the test server and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private static int GetAvailablePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
