#if NETCOREAPP2_2
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;

namespace HttpBatchHandler.Benchmarks.Tools
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseBatchMiddleware();
            app.UseMvc();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }
    }


    public class KestrelHost : IDisposable
    {
        private readonly IWebHost _disposable;

        public KestrelHost()
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
                .ConfigureLogging(logging => { logging.ClearProviders(); })
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

    public class TestHost : IDisposable
    {
        private readonly TestServer _disposable;

        public TestHost()
        {
            var builder = new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                });
            _disposable = new TestServer(builder);
            HttpClient = _disposable.CreateClient();
            HttpClient.BaseAddress = new Uri("http://localhost:5000");
        }

        public Uri BaseUri => HttpClient.BaseAddress;

        public HttpClient HttpClient { get; }

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
#endif