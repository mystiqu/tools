using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Hosting;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

[assembly: WebJobsStartup(typeof(serilogdemo.elastic.Startup))]
namespace serilogdemo.elastic
{
    public enum MESSAGE_TYPE
    {
        TEST_ORDER = 0
    }

    public class ElasticSerilogDemo
    {
        ILogger<ElasticSerilogDemo> logger;
        public ElasticSerilogDemo(ILogger<ElasticSerilogDemo> _logger)
        {
            logger = _logger;
        }

        [FunctionName("ElasticSerilogDemo")]
        public async Task<IActionResult> RunNoCount([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "ElasticSerilogDemo")] HttpRequest req)
        {
            string response = Execute(0);
            return new OkObjectResult(response);
        }

        [FunctionName("ElasticSerilogDemoCount")]
        public async Task<IActionResult> RunCount([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "ElasticSerilogDemo/{count}")] HttpRequest req, 
                                                                                                      int count)
        {
            string response = Execute(count);
            return new OkObjectResult(response);
        }

        private string Execute(int count)
        {
            Random random = new Random();

            MESSAGE_TYPE msgType = MESSAGE_TYPE.TEST_ORDER;
            COUNTRY country = COUNTRY.BOLIVIA;

            for (int i = 0; i < count; i++)
            {
                short messageTypeInt = 5;
                int countryInt = 0;

                countryInt = (int)random.Next(0, 3);
                do
                {
                    messageTypeInt = (short)random.Next(0, 10);
                } while (messageTypeInt == 5);

                msgType = (MESSAGE_TYPE)messageTypeInt;
                country = (COUNTRY)countryInt;


                logger.LogInformation("New {msgType} request from {country}", msgType.ToString(), country);
            }

            Guid flowId = Guid.NewGuid();
            string messagetype = "ARIBA_PO";
            string service = this.GetType().FullName;

            IDictionary<string, object> keyValuePairs = new Dictionary<string, object>();
            keyValuePairs.Add("application", "mikes serilog supah demo");
            keyValuePairs.Add("module", this.GetType().FullName);
            keyValuePairs.Add("senderid", "mike");
            keyValuePairs.Add("receiverid", "glentoft");
            keyValuePairs.Add("primarymessageid", "123");
            keyValuePairs.Add("correlationid", Guid.NewGuid().ToString());

            if (logger.IsEnabled(LogLevel.Trace))
            {
                keyValuePairs.Add("payload", "...");

                logger.LogTrace("My payload");

                keyValuePairs.Remove("payload");
            }

            using (logger.BeginScope(keyValuePairs))
            {
                logger.LogError(new ArgumentException("King Serilog"), "Something less cool happended for {mtype} in {svc} with flowId: {id}", messagetype, service, flowId);
                logger.LogInformation("Something cool happended for {messagetype} in {service} with flowId: {flowid}", messagetype, service, flowId);
            }


            string responseMessage = "Hello Log World";

            return responseMessage;
        }
    }

    enum COUNTRY
    {
        BOLIVIA = 0,
        EQUADOR = 1,
        PERU = 2
    }
}
