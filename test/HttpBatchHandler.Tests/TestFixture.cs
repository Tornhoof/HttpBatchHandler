using System;
using System.Net.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace HttpBatchHandler.Tests
{
    public class TestFixture : IDisposable
    {
        private readonly IWebHost _disposable;

        public TestFixture()
        {
            _disposable = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseUrls(BaseUri.ToString())
                .Build();
            _disposable.Start();
        }

        public Uri BaseUri { get; } = new Uri("http://localhost:12345");

        public HttpClient HttpClient { get; } = new HttpClient();

        public void Dispose()
        {
            _disposable?.Dispose();
            HttpClient?.Dispose();
        }
    }
}