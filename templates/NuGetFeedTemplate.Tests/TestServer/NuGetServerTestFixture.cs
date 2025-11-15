namespace NuGetFeedTemplate.Tests.TestServer;

/// <summary>
/// Base test fixture that provides a fresh NuGet test server instance.
/// Implements IAsyncLifetime to manage server lifecycle per test.
/// </summary>
public class NuGetServerTestFixture : IAsyncLifetime
{
    private NuGetTestServerHost? _server;

    /// <summary>
    /// Gets the test server instance.
    /// </summary>
    protected NuGetTestServerHost Server => _server 
        ?? throw new InvalidOperationException("Server not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Gets the base address of the test server.
    /// </summary>
    public Uri BaseAddress => Server.BaseAddress;

    /// <summary>
    /// Gets the HTTP client configured to communicate with the test server.
    /// </summary>
    public HttpClient Client => Server.Client;

    /// <summary>
    /// Initializes the test server before each test.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        _server = await NuGetTestServerHost.StartAsync();
    }

    /// <summary>
    /// Disposes the test server after each test.
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        if (_server != null)
        {
            await _server.DisposeAsync();
        }
    }
}
