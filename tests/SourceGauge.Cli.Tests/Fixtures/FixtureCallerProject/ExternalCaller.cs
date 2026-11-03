extern alias TargetFixture;

namespace SourceGauge.Cli.Tests.Fixtures.External
{
    internal static class ExternalCaller
    {
        internal static void CallTarget()
        {
            TargetFixture::SourceGauge.Cli.Tests.Fixtures.FanInFixture.Target();
        }
    }
}

namespace SourceGauge.Cli.Tests.Fixtures
{
    internal static class FanInFixture
    {
        internal static void Target()
        {
        }
    }
}
