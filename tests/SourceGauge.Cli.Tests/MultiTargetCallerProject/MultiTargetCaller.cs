using SourceGauge.Cli.Tests.MultiTargetFixtures;

namespace SourceGauge.Cli.Tests.MultiTargetCallerFixtures;

internal static class MultiTargetCaller
{
    internal static void CallTarget()
    {
        MultiTargetFanIn.Target();
    }
}
