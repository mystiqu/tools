using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace mystiqu.elastic.iloggerdemo.logging
{
    public class ColorConsoleLogger : ILogger
    {
        private string _name;
        private ColorConsoleLoggerConfiguration _config;
        private Func<ColorConsoleLoggerConfiguration> _getConfig;

        public ColorConsoleLogger() { }

        public ColorConsoleLogger(string name, ColorConsoleLoggerConfiguration config) 
        {
            _name = name;
            _config = config;
        }

        public ColorConsoleLogger(string name, Func<ColorConsoleLoggerConfiguration> getConfig)
        {
            _name = name;
            _getConfig = getConfig;
        }

        public IDisposable? BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _config.LogLevelToColorMap.ContainsKey(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (_config != null) // && (_config.EventId == 0 || _config.EventId == eventId.Id))
            {
                ConsoleColor originalColor = Console.ForegroundColor;

                Console.ForegroundColor = _config.LogLevelToColorMap[logLevel];
                //Console.WriteLine($"[{eventId.Id,2}: {logLevel,-12}]");

                //Console.ForegroundColor = originalColor;
                //Console.Write($"     {_config} - ");

                //Console.ForegroundColor = _config.LogLevelToColorMap[logLevel];
                //Console.Write($"{formatter(state, exception)}");

                //Console.ForegroundColor = originalColor;
                //Console.WriteLine();
            }

            Console.WriteLine(formatter(state, exception));
        }
    }
}
