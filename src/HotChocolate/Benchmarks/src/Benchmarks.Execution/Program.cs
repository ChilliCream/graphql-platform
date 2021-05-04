using BenchmarkDotNet.Running;
using HotChocolate.Benchmarks;

public static class Program
{
    static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}