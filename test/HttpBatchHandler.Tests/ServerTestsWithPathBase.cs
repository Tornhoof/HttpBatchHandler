using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace HttpBatchHandler.Tests
{
    public class ServerTestsWithPathBase : BaseServerTests<TestFixtureWithPathBase>
    {
        public ServerTestsWithPathBase(TestFixtureWithPathBase fixture, ITestOutputHelper outputHelper) : base(fixture, outputHelper)
        {
        }
    }
}