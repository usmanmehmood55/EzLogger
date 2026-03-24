using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using EzLogger;
using System.Threading.Tasks;

namespace EzLogger.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<LoggerBenchmarks>();
        }
    }

    [MemoryDiagnoser]
    [ShortRunJob] // Still using ShortRun to avoid filling your disk with GBs of logs
    [RankColumn]
    public class LoggerBenchmarks
    {
        private const string LongMessage = "This is a very long log message designed to exceed the initial buffer capacity of our cached string builder to see how the system handles larger allocations during high frequency logging events.";

        [GlobalSetup]
        public void Setup()
        {
            // Silence Console, but process Info and above for File
            Logger.SetConfig(Verbosity.Critical, Verbosity.Info, 10);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            Logger.StopLoggingTasks();
            
            // Clean up the massive benchmark logs
            string logsDir = Path.Combine(Directory.GetCurrentDirectory(), "EzLogger.BenchmarksLogs");
            if (Directory.Exists(logsDir))
            {
                try { Directory.Delete(logsDir, true); } catch {}
            }
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("SingleThread")]
        public void StandardLog()
        {
            Logger.Info("Standard single-line log message.");
        }

        [Benchmark]
        [BenchmarkCategory("SingleThread")]
        public void FilteredLog()
        {
            // This verbosity (Debug) is filtered out by our Setup config.
            // Measures the "early return" performance.
            Logger.Debug("This message will be filtered out.");
        }

        [Benchmark]
        [BenchmarkCategory("SingleThread")]
        public void MultiLineLog()
        {
            Logger.Info("Line 1\nLine 2\nLine 3\nLine 4\nLine 5");
        }

        [Benchmark]
        [BenchmarkCategory("SingleThread")]
        public void LongMessageLog()
        {
            Logger.Info(LongMessage);
        }

        [Benchmark]
        [BenchmarkCategory("Concurrency")]
        public void ParallelLogging_10_Threads()
        {
            // Simulates 10 threads logging 10 messages each simultaneously
            Parallel.For(0, 10, i =>
            {
                for (int j = 0; j < 10; j++)
                {
                    Logger.Info($"Parallel thread {i} message {j}");
                }
            });
        }
    }
}
