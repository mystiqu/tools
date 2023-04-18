using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace FileSplitterDemo.Benchmarks
{

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<PrefixRemover>();
        }
    }

    [SimpleJob(RuntimeMoniker.Net60, baseline: true)]
    [MemoryDiagnoser]
    [RPlotExporter]
    public class PrefixRemover
    {
        const string keyWithPrefix = "-f";
        string _RemovePrefixUsingTrimValue = keyWithPrefix;
        string _RemovePrefixUsingChars = keyWithPrefix;
        char prefix = '-';

        [GlobalSetup]
        public void Setup()
        {

        }


        [Benchmark]
        public void RemovePrefixUsingTrim()
        {
            string internalKey = _RemovePrefixUsingTrimValue;
            if (internalKey.StartsWith(prefix))
                internalKey = internalKey.TrimStart(prefix);
        }

        [Benchmark]
        public void RemovePrefixUsingChar()
        {
            string internalKey = _RemovePrefixUsingChars;
            if (internalKey[0] == prefix)
                internalKey = internalKey.Remove(0);
        }

    }
}
