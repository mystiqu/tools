using System;
using System.Collections.Generic;
using System.Text;
using ConsoleSBReader.parsers;

public enum ACTION
{
    NOTHING = 0,
    BASE64_ENCODE = 1,
    BASE64_DECODE = 2,
}

namespace ConsoleSBReader.factories
{
    public static class ContentFactory
    {
        public static IParser GetParser(string name)
        {
            switch(name.ToLower())
            {
                case "goh":
                    return new GOHPayloadParser();
                default:
                    return null;
            }
        }

        public static IParser GetParser(string name, ACTION action)
        {
            if(string.IsNullOrEmpty(name)) 
                return null;

            switch (name.ToLower())
            {
                case "goh":
                    return new GOHPayloadParser(action);
                default:
                    return null;
            }
        }
    }
}
