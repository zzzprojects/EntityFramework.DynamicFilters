using System;
using BenchmarkDotNet.Running;

namespace DelegateBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<DelegateInvoking>();
            Console.ReadLine();
        }
    }
}
