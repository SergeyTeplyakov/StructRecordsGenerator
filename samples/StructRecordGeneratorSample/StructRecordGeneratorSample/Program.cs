//using System;
//using System.Text;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Iced.Intel;
using StructGenerators;
namespace StructRecordGeneratorSample
{
    //[GenerateToString(PrintTypeName = true)]
    [StructRecord]
    public partial struct StructPoint
    {
        public int X { get; init; }
        public int Y { get; init; }
    }
    
    public record Point
    {
        public int X { get; init; }
        public int Y { get; init; }
    }

    public interface ILogger
    {
        void Always(string format, params object[] args);
    }

    public static class LoggerExtensions
    {
        public static void Always(this ILogger logger, string message)
        { }

        public static void Test(ILogger logger)
        {
            logger.Always("sdfasd");
        }
    }

    class Program
    {
        static async Task FooBarAsync()
        {
            Task task1 = GetTask1();
            Task task2 = GetTask2();

            await task1;
            await task2;
        }

        public static async Task WhenAllWithCancellationAsync(IEnumerable<Task> tasks, CancellationToken token)
        {
            var completedTask = await Task.WhenAny(
                Task.Delay(Timeout.InfiniteTimeSpan, token),
                Task.WhenAll(tasks));
            
            token.ThrowIfCancellationRequested();
            await completedTask;
        }

        private static async Task GetTask2()
        {
            await Task.Yield();
            //lock (new object())
            //{
            //    await Task.Yield();
            //}
        }
        
        

        private static Task GetTask1()
        {
            throw new NotImplementedException();
        }

        public class RemoteWorker : IDisposable
        {
            public async Task FinishAsync()
            {
                Console.WriteLine("FinishAsync");
                throw null;
            }

            public void Dispose()
            {
                Console.WriteLine("Dispose");
            }
        }

        static void NotCorrectVersion()
        {
            var list = Enumerable.Range(1, 5).Select(_ => new RemoteWorker()).ToList();

            try
            {
                // The following code is not safe because the task produced by FinishAsync is never observed.
                // So this will generate Unobserved task error and WaitAll will fail only if Dispose method will fail.
                Task.WaitAll(list.Select(static w => w
                    .FinishAsync()
                    .ContinueWith(_ =>
                    {
                        w.Dispose();
                    })
                ).ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e);
            }
        }

        static void CorrectVersion()
        {
            var list = Enumerable.Range(1, 5).Select(_ => new RemoteWorker()).ToList();

            try
            {
                // The following code is not safe because the task produced by FinishAsync is never observed.
                // So this will generate Unobserved task error and WaitAll will fail only if Dispose method will fail.
                Task.WaitAll(list.Select(static async w =>
                {
                    using (w)
                    {
                        await w.FinishAsync();

                    }
                }).ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e);
            }
        }

        static void Main(string[] args)
        {
            TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
                Console.WriteLine($"Unobserved error: {eventArgs.Exception}");

            //NotCorrectVersion();
            CorrectVersion();
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
