using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mystiqu.elastic.iloggerdemo.logging
{
    [ProviderAlias("ColorConsole")]
    public class ColorConsoleLoggerProvider : ILoggerProvider
    {
        ColorConsoleLoggerConfiguration _configuration;
        private readonly IDisposable? _onChangeToken;
        private readonly ConcurrentDictionary<string, ColorConsoleLogger> _loggers =
        new ConcurrentDictionary<string, ColorConsoleLogger>();

        public ColorConsoleLoggerConfiguration Configuration { get { return _configuration;  } }

        public ColorConsoleLoggerProvider()
        {

        }

        public ColorConsoleLoggerProvider(ColorConsoleLoggerConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ColorConsoleLoggerProvider(IOptionsMonitor<ColorConsoleLoggerConfiguration> config) 
        {
            _configuration = config.CurrentValue;
            _onChangeToken = config.OnChange(updatedConfig => _configuration = updatedConfig);
        }


        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new ColorConsoleLogger(name, _configuration));
        }

        public void Dispose()
        {
            _loggers.Clear();
            _onChangeToken?.Dispose();
        }
    }
}
