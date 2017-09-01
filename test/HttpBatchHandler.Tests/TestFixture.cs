﻿using System;
using System.Net.Http;
using HttpBatchHandler.Website;
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

        public HttpClient HttpClient { get; } = new HttpClient();
        public Uri BaseUri { get; } = new Uri("http://localhost:12345");

        public void Dispose()
        {
            _disposable?.Dispose();
            HttpClient?.Dispose();
        }
    }
}