using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Logging;
using Serilog.Sinks.Elasticsearch;
using System;

namespace serilogdemo.elastic
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            //Add ApplicationInsights
            string applicationInsightsInstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"); //APPINSIGHTS_CONNECTIONSTRING
            string elasticSearchProxyUrl = Environment.GetEnvironmentVariable("ELASTIC_PROXY_URL"); 
            var environment = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) ? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") : "DEBUG";

            // Registering Serilog provider
            var seriLogger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Environment", "DEV")
                .WriteTo.Console()
                .WriteTo.File("serilog.txt")
                //.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticSearchProxyUrl))
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("https://pre-es.****************.org:9200"))
                //.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:7071/api/elasticproxy"))                        //local
                //.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("https://test-elasticlogging.****************.org:9200/glentoft/_doc"))  //new
                //.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://test-elasticsearch.****************.org:9200"))                  //old
                {
                    ModifyConnectionSettings = x => x.BasicAuthentication("GOH", "GlobalOrderHub2020"), //preprod, vision
                    //ModifyConnectionSettings = x => x.BasicAuthentication("elastic", "kibana"), //test
                    AutoRegisterTemplate = true,
                    IndexFormat = "glentoft-{0:yyyyMM}"
                })
                //.Enrich.WithProperty("Environment", environment)
                .CreateLogger();

            builder.Services.AddLogging(logger =>
                   logger.AddSerilog(seriLogger));

            if (!string.IsNullOrEmpty(applicationInsightsInstrumentationKey))
            {
                //builder.Services.AddLogging(builder =>
                //{
                //    //builder.AddApplicationInsights(applicationInsightsInstrumentationKey);

                //    //This line does not work in conjunction with AddFilter(...)
                //    builder.SetMinimumLevel(LogLevel.Trace);                                        //Overrides logging.logLevel.default in host.json 

                //    //This line overides the AILoggerProvider for same scope and the minimumLogLevel
                //    //It also overrides ev. same row in host.json
                //    //This overrides heven and hell, use it for now!
                //    builder.AddFilter<ApplicationInsightsLoggerProvider>("AITestConfigs.SerilogTest.SerilogDemo", LogLevel.Trace);         //Function specific log level 
                //    builder.AddFilter<ApplicationInsightsLoggerProvider>("Function.SerilogDemo", LogLevel.Trace);         //Function specific log level 
                //    builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Error);
                //});

                //builder.Services.AddApplicationInsightsTelemetry();                                     //Not sure this does anything...?
            }

            builder.Services.AddMvcCore();
            builder.Services.AddOptions();
         }
    }
}
