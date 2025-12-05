namespace Mcp.Gateway.Benchmarks;

using BenchmarkDotNet.Running;


public class Program
{
    public static void Main(string[] args)
    {
        // Run all benchmarks
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
