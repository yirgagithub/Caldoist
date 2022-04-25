using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.AspNetCore3;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic.CompilerServices;
using TodoistCalendarSync.DataContext;
using TodoistCalendarSync.Services;

namespace  Hopesoftware.TodoistCalendarSync
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
            services.AddAuthentication(o =>
            {
                o.DefaultChallengeScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
                o.DefaultForbidScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
        .AddCookie()
        .AddGoogleOpenIdConnect(options =>
        {
            options.ClientId = "802184794860-tqj26acicng2uoapedotah6j3bn2i0eo.apps.googleusercontent.com";
            options.ClientSecret = "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";
        });

           services.AddMvc().AddNewtonsoftJson();


            services.AddSingleton<ApplicationDbContext>();
            services.AddScoped<IUserSession, UserSession>();
            services.AddScoped<IIntegrationInterface, IntegrationService>();
            services.AddScoped<GoogleCalendarInterface, GoogleCalendarImp>();
            services.AddScoped<TodoistInterface, TodoistImp>();
            services.AddControllersWithViews();
            services.AddHttpContextAccessor();
            //services.AddHostedService<ListenSmee>();
           // services.AddHostedService<ListenSmeeGC>();

            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
