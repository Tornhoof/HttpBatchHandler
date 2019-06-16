using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HttpBatchHandler.Benchmarks.Tools
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseBatchMiddleware();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddControllers();
        }
    }


    public class TestHost : IDisposable
    {
        private readonly IWebHost _disposable;

        public TestHost()
        {
            var port = RandomPortHelper.FindFreePort();
            BaseUri = new Uri($"http://localhost:{port}");
            var url = new UriBuilder(BaseUri)
            {
                Path = string.Empty
            };

            _disposable = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseUrls(url.Uri.ToString())
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                })
                .Build();
            _disposable.Start();
        }

        public Uri BaseUri { get; }

        public HttpClient HttpClient { get; } = new HttpClient();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                _disposable?.Dispose();
                HttpClient?.Dispose();
            }
        }
    }
}