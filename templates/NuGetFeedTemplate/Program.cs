using Microsoft.AspNetCore.Builder;
using NuGetFeedTemplate;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine("App_Data", "Logs", "log.txt"))
    .CreateLogger();

await WebApplication.CreateBuilder(args)
    .ConfigureAndRunAsync();
