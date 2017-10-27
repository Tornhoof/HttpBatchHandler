namespace HttpBatchHandler.Tests
{
    public class TestFixtureWithPathBase : TestFixture
    {
        public TestFixtureWithPathBase() : base("/path/base/")
        {
        }
    }
}