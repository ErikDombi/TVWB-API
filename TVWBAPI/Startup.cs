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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            GlobalConfiguration.Configuration.UseSQLiteStorage("TVWBAPI.db");
            //services.AddHangfire(x => x.UseSimpleAssemblyNameTypeSerializer().UseRecommendedSerializerSettings().UseSQLiteStorage("TVWBAPI.db"));
            //services.AddHangfireServer();
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
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHangfireDashboard();
            app.UseMvc();
            //RecurringJob.AddOrUpdate(() => TimetableUpdaterTask.Update(), Cron.Daily);
            //RecurringJob.AddOrUpdate(() => AttendanceUpdaterTask.Update(), Cron.Hourly);
        }
    }
}
