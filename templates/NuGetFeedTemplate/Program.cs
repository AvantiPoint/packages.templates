using Microsoft.AspNetCore.Builder;
using NuGetFeedTemplate;
using NuGetFeedTemplate.Data;

var builder = WebApplication.CreateBuilder(args);
Startup.ConfigureServices(builder.Services, builder.Configuration);
var app = builder.Build();
await app.InitializeDatabaseContext();
Startup.Configure(app, app.Environment);
await app.RunAsync();
