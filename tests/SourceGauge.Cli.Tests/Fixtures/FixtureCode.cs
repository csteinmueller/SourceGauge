namespace SourceGauge.Cli.Tests.Fixtures;

internal static class FixtureCode
{
    internal static int Value => 1;
}

internal static partial class PartialFanInFixture
{
    internal static partial void Target();

    internal static partial void Target()
    {
    }

    internal static void Caller()
    {
        Target();
    }
}

public class FanInFixture
{
    public FanInFixture()
    {
        Target();
    }

    static FanInFixture()
    {
        Target();
    }

    public static void Target()
    {
    }

    internal static void RepeatedCaller()
    {
        Target();
        Target();
    }

    internal static void DistinctCaller()
    {
        Target();
    }

    internal static void Overloaded(int value)
    {
    }

    internal static void Overloaded(string value)
    {
    }

    internal static void CallsIntOverloadA()
    {
        Overloaded(1);
    }

    internal static void CallsIntOverloadB()
    {
        Overloaded(2);
    }

    internal static void CallsStringOverload()
    {
        Overloaded("value");
    }

    internal static void SelfRecursive()
    {
        SelfRecursive();
    }

    internal static void MutualA()
    {
        MutualB();
    }

    internal static void MutualB()
    {
        MutualA();
    }

    internal static void CallsConstructor()
    {
        _ = new Constructed();
    }

    internal static void CallsInstanceConstructor()
    {
        _ = new FanInFixture();
    }

    internal static void DynamicTarget()
    {
    }

    internal static void LocalFunctionOnlyTarget()
    {
    }

    internal static void LambdaOnlyTarget()
    {
    }

    internal static void CallsDynamically(dynamic target)
    {
        target.DynamicTarget();
    }

    internal static void CallsFromLocalFunction()
    {
        void LocalCaller()
        {
            LocalFunctionOnlyTarget();
        }

        LocalCaller();
    }

    internal static void CallsFromLambda()
    {
        System.Action caller = () => LambdaOnlyTarget();
        caller();
    }

    internal sealed class Constructed
    {
        internal Constructed()
        {
        }
    }

    internal interface IDispatchTarget
    {
        void Run();
    }

    internal sealed class DispatchImplementation : IDispatchTarget
    {
        public void Run()
        {
        }
    }

    internal static void CallsInterface(IDispatchTarget target)
    {
        target.Run();
    }

    internal static void GeneratedOnlyTarget()
    {
    }
}
