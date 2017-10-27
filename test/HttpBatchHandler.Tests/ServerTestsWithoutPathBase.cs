using Xunit.Abstractions;

namespace HttpBatchHandler.Tests
{
    public class ServerTestsWithoutPathBase : BaseServerTests<TestFixtureWithoutPathBase>
    {
        public ServerTestsWithoutPathBase(TestFixtureWithoutPathBase fixture, ITestOutputHelper outputHelper) : base(
            fixture, outputHelper)
        {
        }
    }
}