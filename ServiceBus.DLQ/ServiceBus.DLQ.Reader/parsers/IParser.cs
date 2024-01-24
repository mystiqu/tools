using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleSBReader.parsers
{
    public interface IParser
    {
        string Parse(string content);
    }
}
