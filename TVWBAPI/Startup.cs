using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hangfire;
using Hangfire.Storage.SQLite;
using TVWBAPI.TVDSB;
using AspNetCore.RouteAnalyzer;

namespace TVWBAPI
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
            services.AddSingleton(new UserManager());
            services.AddSingleton(new Authentication());
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            GlobalConfiguration.Configuration.UseSQLiteStorage("TVWBAPI.db");
            services.AddHangfire(x => x.UseSimpleAssemblyNameTypeSerializer().UseRecommendedSerializerSettings().UseSQLiteStorage("TVWBAPI.db"));
            services.AddHangfireServer();
            services.AddSingleton(new NotificationHandler());
            services.AddCors(o =>
            {
                o.AddPolicy("Public", builder =>
                {
                    builder.AllowAnyOrigin();
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                });

                o.AddPolicy("Private", builder =>
                {
                    builder.WithOrigins("https://auth.thamesvalleywebportal.com");
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                });
            });
            services.AddRouteAnalyzer();
        }

        AttendanceUpdaterTask AUT;
        UserManager pubUM;
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime, NotificationHandler NH, UserManager UM, Authentication Auth)
        {
            Auth = new Authentication(UM);
            pubUM = UM;
            applicationLifetime.ApplicationStarted.Register(OnShutDown);
            AUT = new AttendanceUpdaterTask(NH, UM, Auth);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHangfireDashboard();
            app.UseMvc(r =>
            {
                r.MapRouteAnalyzer("/routes");
            });
            //RecurringJob.AddOrUpdate(() => TimetableUpdaterTask.Update(NH), Cron.Daily);
            RecurringJob.AddOrUpdate(() => AUT.Update(), Cron.Hourly);
            RecurringJob.AddOrUpdate(() => UM.Save(), Cron.Minutely);
        }

        private void OnShutDown()
        {
            pubUM.Save();
        }
    }
}
