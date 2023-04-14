using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mystiqu.tools.files.splitters;
using BenchmarkDotNet.Running;

namespace FileSplitterDemo.Benchmarks
{

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<FileSplitterBenchmarker>();
        }
    }

    [SimpleJob(RuntimeMoniker.Net60, baseline: true)]
    [MemoryDiagnoser]
    [RPlotExporter]
    public class FileSplitterBenchmarker
    {
        FileSplitter? splitter;

        [GlobalSetup]
        public void Setup()
        {
            splitter = new FileSplitter(@"C:\Temp\test-split.sql", 1);
        }


        //[Benchmark]
        //public void SplitFile() => splitter.Split();

        //[Benchmark]
        public void WriteLineUsingMethod()
        {
            StreamReader sr = new StreamReader(@"C:\Temp\test-split.sql");
            StringWriter stringWriter;
            string line = null;
            stringWriter = new StringWriter();

            line = sr.ReadLine();
            WriteLine(ref stringWriter, line);

            stringWriter.Close();
            sr.Close();
        }

        [Benchmark]
        public void WriteAllUsingReadAllMethod()
        {
            StreamReader sr = new StreamReader(@"C:\Temp\test-split-medium.sql");
            StringWriter stringWriter;
            string line = null;
            stringWriter = new StringWriter();

            line = sr.ReadToEnd();
            WriteLine(ref stringWriter, line);

            stringWriter.Close();
            sr.Close();
        }

        [Benchmark]
        public void WriteAllUsingReadLineAndNoStringMethod()
        {
            StreamReader sr = new StreamReader(@"C:\Temp\test-split-medium.sql");
            StringWriter stringWriter;
            stringWriter = new StringWriter();

            do
            {
                stringWriter.Write(sr.ReadLine() + Environment.NewLine);
            } while (!sr.EndOfStream);

            stringWriter.Close();
            sr.Close();
        }

        [Benchmark]
        public void WriteAllUsingReadLineMethod()
        {
            StreamReader sr = new StreamReader(@"C:\Temp\test-split-medium.sql");
            StringWriter stringWriter;
            string line = null;
            stringWriter = new StringWriter();

            do
            {
                line = sr.ReadLine();
                WriteLine(ref stringWriter, line);
            } while (line != null);
            

            stringWriter.Close();
            sr.Close();
        }

        //[Benchmark]
        public void WriteLineUsingInline()
        {

            StreamReader sr = new StreamReader(@"C:\Temp\test-split-01.sql");
            StringWriter stringWriter;
            string line = null;
            stringWriter = new StringWriter();

            line = sr.ReadLine();

            if (line == null)
                return;
            else //if (!line.Equals(""))
                stringWriter.Write(line + Environment.NewLine);

            stringWriter.Close();
            sr.Close();

        }

        private bool WriteLine(ref StringWriter writer, string line)
        {
            if (line == null)
                return true;
            else //if (!line.Equals(""))
                writer.Write(line + Environment.NewLine);

            return false;
        }
    }
}
