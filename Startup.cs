using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Budget.Lib;
using Microsoft.AspNetCore.Http;

namespace Budget
{
    public class SharedResource
    {
    }

    public class MongoDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var settings = new MongoDbSettings
            {
                ConnectionString = Configuration["MongoDb:ConnectionString"],
                DatabaseName = Configuration["MongoDb:DatabaseName"]
            };
            services.AddSingleton(s => settings);

            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.AddAuthorization(opts => {
                opts.AddPolicy("OnlyForLondon", policy => {
                    policy.RequireClaim(ClaimTypes.Locality, "Лондон", "London");
                });
                opts.AddPolicy("OnlyForMicrosoft", policy => {
                    policy.RequireClaim("company", "Microsoft");
                });
            });

            services.AddMvc()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            var supportedCultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("ru"),
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("ru"),
                // Formatting numbers, dates, etc.
                SupportedCultures = supportedCultures,
                // UI strings that we have localized.
                SupportedUICultures = supportedCultures
            });



            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {
                AuthenticationScheme = "Cookies",
                LoginPath = new PathString("/Home/"),
                //AccessDeniedPath = new PathString("/Account/Forbidden/"),
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = CookiesValidator.ValidateAsync
                }
        });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "Account",
                    template: "account/{action=Login}/{param?}",
                    defaults: new { controller = "Account" });
                routes.MapRoute(
                    name: "Site",
                    template: "site/{action=Index}/{param?}",
                    defaults: new { controller = "Site" });
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Site}/{action=Index}/{param?}");
            });
        }
    }
}
