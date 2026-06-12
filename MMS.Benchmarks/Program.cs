using BenchmarkDotNet.Running;

namespace MMS.Benchmarks;

internal static class Program
{
    public static void Main()
    {
        BenchmarkRunner.Run<FilterBenchmarks>();
    }
}
