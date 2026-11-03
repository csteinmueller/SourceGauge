namespace SourceGauge.Cli.Tests.MultiTargetFixtures;

internal static class MultiTargetFanIn
{
    internal static void Target()
    {
    }

#if NET10_0
    internal static void Caller()
    {
        Target();
    }
#endif
}
