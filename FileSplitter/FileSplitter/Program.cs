// See https://aka.ms/new-console-template for more information
using FileSplitterDemo.Splitters;
using System.Diagnostics;
using System.IO;

public class Program
{
    private static void Main(string[] args)
    {
        FileSplitter splitter = new FileSplitter(@"C:\Temp\test-split-large-advanced.sql", 10000);
        splitter.Split();
     
    }
}