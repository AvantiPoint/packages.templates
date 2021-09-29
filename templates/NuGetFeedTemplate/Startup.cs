using System.Security.Claims;
using System.Threading.Tasks;
using AvantiPoint.Packages;
using AvantiPoint.Packages.Hosting;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;
using NuGetFeedTemplate.Services;

namespace NuGetFeedTemplate
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddNuGetPackagApi(options =>
            {
                if (options.IsDevelopment)
                    options.AddFileStorage();
                else
                    options.AddAzureBlobStorage();

                options.AddFeedConfiguration()
                   .AddFeedServices()
                   .AddSqlServerDatabase("DefaultConnection");
            });

            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"));

            services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = OnTokenValidated
                };
            });

            services.AddAuthorization(options =>
            {
                // By default, all incoming requests will be authorized according to the default policy
                options.FallbackPolicy = options.DefaultPolicy;
            });
            services.AddRazorPages()
                .AddMicrosoftIdentityUI();

            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = int.MaxValue;
            });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
            });

            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // if don't set default value is: 128 MB
                x.MultipartHeadersLengthLimit = int.MaxValue;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
//#if DEBUG
//                using var scope = app.ApplicationServices.CreateScope();
//                using var db = scope.ServiceProvider.GetRequiredService<FeedContext>();
//                db.Database.EnsureCreated();
//                using var db2 = scope.ServiceProvider.GetRequiredService<SqlServerContext>();
//                db2.Database.Migrate();
//#endif
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseOperationCancelledMiddleware();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapNuGetApiRoutes();
            });
        }

        private async Task OnTokenValidated(TokenValidatedContext ctx)
        {
            var feedContext = ctx.HttpContext.RequestServices.GetRequiredService<FeedContext>();
            var email = ctx.Principal.FindFirstValue("preferred_username");
            var user = await feedContext.Users.FirstOrDefaultAsync(x => x.Email == email);
            if(user is null)
            {
                user = new User
                {
                    Email = email,
                    Name = ctx.Principal.FindFirstValue("name"),
                    PackagePublisher = !await feedContext.Users.AnyAsync()
                };
                feedContext.Users.Add(user);
                await feedContext.SaveChangesAsync();
            }

            if(user.PackagePublisher)
            {
                var claimsIdentity = ctx.Principal.Identity as ClaimsIdentity;
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
            }
        }
    }
}
