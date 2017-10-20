using System;
using System.Net.Http;
using HttpBatchHandler.Website;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace HttpBatchHandler.Tests
{
    public abstract class TestFixture : IDisposable
    {
        private readonly IWebHost _disposable;

        public TestFixture(string pathBase)
        {
            var port = RandomPortHelper.FindFreePort();
            BaseUri = new Uri($"http://localhost:{port}" + pathBase);
            var url = new UriBuilder(BaseUri)
            {
                Path = string.Empty
            };

            _disposable = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseUrls(url.Uri.ToString())
                .UseSetting("pathBase", pathBase)
                .Build();
            _disposable.Start();
        }

        public Uri BaseUri { get; }

        public HttpClient HttpClient { get; } = new HttpClient();

        public virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                _disposable?.Dispose();
                HttpClient?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}