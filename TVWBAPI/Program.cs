using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TVWBAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (!File.Exists("users.json"))
                File.Create("users.json").Close();
            if (String.IsNullOrEmpty(StaticFunctions.UsersRaw))
                File.WriteAllText("users.json", "[]");
            if (!File.Exists("apps.json"))
                File.Create("apps.json").Close();
            if (String.IsNullOrEmpty(StaticFunctions.AppsRaw))
                File.WriteAllText("apps.json", "[]");
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://localhost:9385/")
                .UseStartup<Startup>();
    }
}
