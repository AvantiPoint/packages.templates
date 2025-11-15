# NuGet Feed Template Tests

This directory contains integration tests for the NuGet Feed Template using an in-process test server.

## Overview

The test infrastructure provides a reusable, in-process NuGet server for protocol-level testing. Tests can stand up isolated server instances and run real HTTP scenarios (push, list, download, etc.) without external dependencies.

## Key Components

### NuGetTestServerHost

The main test server host that starts an in-process NuGet server on a random free port.

**Usage:**

```csharp
await using var server = await NuGetTestServerHost.StartAsync();
var client = server.Client;
var baseAddress = server.BaseAddress;

// Make requests to the server
var response = await client.GetAsync("/v3/index.json");
```

**Features:**
- Starts server on random free port (avoids conflicts in parallel test runs)
- Uses in-memory database for fast, isolated tests
- Implements `IAsyncDisposable` for proper cleanup
- Fully configured with NuGet v2/v3 endpoints

### NuGetServerTestFixture

Base test fixture that implements `IAsyncLifetime` for xUnit test classes.

**Usage:**

```csharp
public class MyTests : IClassFixture<NuGetServerTestFixture>
{
    private readonly NuGetServerTestFixture _fixture;

    public MyTests(NuGetServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MyTest()
    {
        var client = _fixture.Client;
        var baseAddress = _fixture.BaseAddress;
        // ... test implementation
    }
}
```

**Features:**
- Fresh server instance per test class
- Automatic startup and cleanup
- Exposes `BaseAddress` and `Client` properties

### TestPackageHelper

Utility class for creating and seeding test packages.

**Creating packages:**

```csharp
// Create a single package
var packageBytes = TestPackageHelper.CreatePackage("MyPackage", "1.0.0");

// Create multiple test packages
var packages = TestPackageHelper.CreateTestPackages();
// Returns: Test.PackageA 1.0.0, Test.PackageA 1.1.0, Test.PackageB 2.0.0
```

**Pushing packages:**

```csharp
var response = await TestPackageHelper.PushPackageAsync(
    client, 
    "Test.PackageA", 
    "1.0.0");
```

## Configuration

The test server uses:
- **In-memory database**: Fast, isolated storage using Entity Framework Core In-Memory provider
- **File storage**: Temporary directory for package files
- **No authentication**: Tests run without auth requirements for simplicity
- **Random ports**: Each server instance uses a different port to avoid conflicts

## Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter ClassName=NuGetServerTests
```

## Test Isolation

Each test class gets its own isolated server instance with:
- Unique in-memory database
- Unique port
- Separate HTTP client
- Independent package storage

This ensures tests don't interfere with each other and can run in parallel.

## CI Integration

Tests are designed to run in CI environments:
- No external network calls (fully in-process)
- No port conflicts (random port allocation)
- Fast execution (in-memory database)
- Deterministic results (isolated per test)

## Example Tests

See `NuGetServerTests.cs` and `PackageProtocolTests.cs` for examples of:
- Server startup verification
- Endpoint testing
- Package creation and pushing
- Isolated server instances
