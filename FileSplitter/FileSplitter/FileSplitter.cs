using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileSplitterDemo.Splitters
{
    public enum SPLIT_FILE_NAMING_RULE
    {
        APPEND_NUMBER = 0,
        APPEND_SUFFIX_AND_NUMBER = 1
    }
    public class FileSplitter
    {
        private StreamReader _inputStreamReader;
        private StringWriter _outputStringWriter;
        private FileStream _outputFileStream;
        private int _flushEveryNLine = 1000;

        public SPLIT_FILE_NAMING_RULE SplitNameingRule { get; set; }
        public string FileName { get; set; }
        public int NumberOfLines { get; set; }
        public string SplitFileSuffix { get; set; }
        public int MaxSizePerFileKiloBytes { get; set; }

        public Regex EndOfLineExpression{ get; set; }

        public string EndOfLineString { get; set; }

        public FileSplitter(string fileName, int numberOfLines)
        {
            FileName = fileName;
            NumberOfLines = numberOfLines;
            SplitFileSuffix = "";
            SplitNameingRule = SPLIT_FILE_NAMING_RULE.APPEND_NUMBER;
            MaxSizePerFileKiloBytes = 10000; //100000 KB = 100MB
        }

        public FileSplitter(string fileName, int numberOfLines, Regex endOfLine) : this(fileName, numberOfLines)
        {
            EndOfLineExpression = endOfLine;
        }

        public FileSplitter(string fileName, int numberOfLines, Regex endOfLine, SPLIT_FILE_NAMING_RULE namingRule) : this(fileName, numberOfLines, endOfLine)
        {
            SplitNameingRule = namingRule;
        }

        public FileSplitter(string fileName, int numberOfLines, Regex endOfLine, SPLIT_FILE_NAMING_RULE namingRule, string splitFileSuffix) : this(fileName, numberOfLines, endOfLine, namingRule)
        {
            SplitFileSuffix = splitFileSuffix;
        }

        public void Split()
        {
            _inputStreamReader = new StreamReader(FileName);

            string? line = null;
            int count = 1;
            int currentLine = 0;
            string splitFileName;

            do
            {
                currentLine = 0;
                _outputStringWriter = new StringWriter();
                splitFileName = GenerateFileName(count);

                do
                {
                    line = _inputStreamReader.ReadLine();
                    if (WriteLine(ref _outputStringWriter, line))
                        break;

                    if ((currentLine+1) % _flushEveryNLine == 0)
                        Flush(splitFileName, ref _outputStringWriter);

                    currentLine++;

                } while (!IsLastLine(currentLine, line));

                SaveFile(splitFileName, ref _outputStringWriter);
                _outputStringWriter.Close();

                count++;
            } while (line != null);

            _inputStreamReader.Close();
            _outputFileStream.Close();
        }

        private bool IsLastLine(int currentLine, string? line)
        {
            //First, check the size
            long size = (_outputFileStream != null && _outputFileStream.CanWrite) ? _outputFileStream.Length : 0;

            if(MaxSizePerFileKiloBytes > 0 && size > MaxSizePerFileKiloBytes*1000)
                return IsEndOfLine(line);
            else if (currentLine < NumberOfLines)
                return false;
            else
                return IsEndOfLine(line);
        }

        private string GenerateFileName(int count)
        {
            string newFileName;
            int startOfFileExtension = FileName.LastIndexOf(".");
            string fileExtension = FileName.Substring(startOfFileExtension);

            return FileName.Substring(0, startOfFileExtension) +
                "-" + count.ToString().PadLeft(2, '0') + fileExtension;
        }

        private bool IsEndOfLine(string? line)
        {
            if (!string.IsNullOrEmpty(EndOfLineString))
                return line.EndsWith(EndOfLineString);
            else if(EndOfLineExpression != null)
                return EndOfLineExpression.IsMatch(line);

            return true;
        }
        private bool WriteLine(ref StringWriter writer, string? line)
        {
            if (line == null)
                return true;
            else //if (!line.Equals(""))
                writer.Write(line + Environment.NewLine);
           
            return false;
        }
        private void SaveFile(string fileName, ref StringWriter writer)
        {
            if(writer.GetStringBuilder().Length != 0)
            {
                if (_outputFileStream == null || !_outputFileStream.CanWrite)
                    _outputFileStream = new FileStream(fileName, FileMode.Create);
                _outputFileStream.Write(System.Text.Encoding.UTF8.GetBytes(writer.ToString()));
            }
            _outputFileStream.Close();
        }

        private void Flush(string fileName, ref StringWriter writer)
        {
            if (_outputFileStream == null || !_outputFileStream.CanWrite)
                _outputFileStream = new FileStream(fileName, FileMode.Create);
            _outputFileStream.Write(System.Text.Encoding.UTF8.GetBytes(writer.ToString()));
            writer.GetStringBuilder().Clear();
        }
    } 
}
