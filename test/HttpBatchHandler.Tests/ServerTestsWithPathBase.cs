using Xunit.Abstractions;

namespace HttpBatchHandler.Tests
{
    public class ServerTestsWithPathBase : BaseServerTests<TestFixtureWithPathBase>
    {
        public ServerTestsWithPathBase(TestFixtureWithPathBase fixture, ITestOutputHelper outputHelper) : base(fixture,
            outputHelper)
        {
        }
    }
}