using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace HttpBatchHandler.Website
{
    public class Program
    {
        public static IWebHost BuildWebHost(string[] args) => WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .UseUrls("http://*:5123")
            .Build();

        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }
    }
}