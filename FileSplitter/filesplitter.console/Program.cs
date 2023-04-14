// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.IO;
using mystiqu.tools.files.splitters;

public class Program
{
    private static void Main(string[] args)
    {
        FileSplitter splitter = new FileSplitter(@"C:\Temp\products-data\products-productInstance-services-01.sql", 400000);
        splitter.MaxSizePerFileKiloBytes = 50000;
        splitter.Split();
    }
}