using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Hosting;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.Extensions.Primitives;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

[assembly: WebJobsStartup(typeof(serilogdemo.elastic.Startup))]
namespace serilogdemo.elastic
{
    public class ElasticProxy
    {
        private IConfiguration _configuration;
        private string _elasticBaseUrl;
        ILogger<ElasticSerilogDemo> _logger;

        public ElasticProxy(IConfiguration configuration, ILogger<ElasticSerilogDemo> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _elasticBaseUrl = _configuration["ELASTIC_URL"];
            if (!string.IsNullOrEmpty(_elasticBaseUrl) && !_elasticBaseUrl.EndsWith('/'))
                _elasticBaseUrl = _elasticBaseUrl + "/";
        }

        [FunctionName("servicenow")]
        public async Task<IActionResult> ServiceNow([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post",
                                                            Route = "servicenow")] HttpRequest req)
        {
            _logger.LogInformation("Received ServiceNow Request");
            return new OkResult();
        }

        [FunctionName("elasticproxybulk")]
        public async Task<IActionResult> ProxyBulk([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post",
                                                            Route = "elasticproxy/_bulk")] HttpRequest req)
        {
            string response = await ProcessRequest(req);

            return new OkObjectResult(response);
        }

        [FunctionName("elasticproxybulkwithindex")]
        public async Task<IActionResult> ProxyIndexBulk([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post",
                                                            Route = "elasticproxy/{index}/_bulk")] HttpRequest req,
                                                            string index)
        {
            string response = await ProcessRequest(req);

            return new OkObjectResult(response);
        }

        [FunctionName("elasticproxydoc")]
        public async Task<IActionResult> ProxySingle([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post",
                                                      Route = "elasticproxy/_doc")] HttpRequest req)
        {
            string response = await ProcessRequest(req);

            return new OkObjectResult(response);
        }

        [FunctionName("elasticproxydocwithindex")]
        public async Task<IActionResult> ProxyIndexSingle([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post",
                                                           Route = "elasticproxy/{index}/_doc")] HttpRequest req,
                                                           string index)
        {
            string response = await ProcessRequest(req);

            return new OkObjectResult(response);
        }

        private async Task<string> ProcessRequest(HttpRequest req)
        {
            _logger.LogInformation("Processing elastic proxy request");
            HttpClient client = HttpClientFactory.Create();
            HttpContent content = new StringContent(GetContent(req.Body));

            string request = await content.ReadAsStringAsync();
            
            //Pipe the request to a secondary async endpoint for ERROR processing
            //... piping piping ....  :) 
            List<JObject> errors = ParseRequestAndProcessErrors(request);

            SetHeaders(ref client, ref content, req.Headers);

            string reqUri = _elasticBaseUrl + req.HttpContext.Request.Path.Value.Replace("/api/elasticproxy/", "");
            _logger.LogInformation("Relaying to '{elasticurl}'", reqUri);

            HttpResponseMessage msg = await client.PostAsync(reqUri, content);
            string response = await msg.Content.ReadAsStringAsync();

            return response;
        }

        private List<JObject> ParseRequestAndProcessErrors(string body)
        {
            List<JObject> objects = GetJObjects(body);
            List<JObject> errors = new List<JObject>();
            JObject currentIndex = null;

            foreach (JObject obj in objects)
            {
                JProperty parentProp = (JProperty)obj.First;
                string name = parentProp.Name;  // "index"
                if(name.ToLower().Equals("index"))
                {
                    currentIndex = obj;
                    //Just the index object, ignore
                    continue;
                }
                else
                {
                    string value = (string)((JProperty)obj["level"].Parent).Value;
                    if(value.ToLower().Equals("error"))
                    {
                        //Hooraa - we got an error - do the fancy dance
                        errors.Add(currentIndex);
                        errors.Add(obj);
                    }
                }
            }

            return errors;
        }

        private List<JObject> GetJObjects(string body)
        {
            List<JObject> result = new List<JObject>();
            try
            {
                string[] elements = body.Split(new char[] { '\n' });
                foreach (string element in elements)
                    result.Add(JObject.Parse(element));
            }
            catch (Exception ex)
            {

            }

            return result;
        }

        public static async Task<HttpResponseMessage> CopyProxyHttpResponse(HttpResponseMessage responseMessage)
        {
            if (responseMessage == null)
            {
                throw new ArgumentNullException(nameof(responseMessage));
            }

            HttpResponseMessage response = new HttpResponseMessage();

            response.StatusCode = responseMessage.StatusCode;
            foreach (var header in responseMessage.Headers)
            {
                response.Headers.Add(header.Key.ToString(), header.Value.ToArray());
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                response.Headers.Add(header.Key.ToString(), header.Value.ToArray());
            }

            // SendAsync removes chunking from the response. This removes the header so it doesn't expect a chunked response.
            response.Headers.Remove("transfer-encoding");

            using (var responseStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                StreamReader reader = new StreamReader(responseStream);

                MemoryStream ms = new MemoryStream();
                responseStream.CopyTo(ms);
                byte[] content =  ms.ToArray();

                response.Content = new ByteArrayContent(content);
            }

            return response;
        }

        public static HttpRequestMessage CreateProxyHttpRequest(HttpRequest proxyRequest, Uri uri)
        {
            var requestMessage = new HttpRequestMessage();
            var requestMethod = proxyRequest.Method;
            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(proxyRequest.Body);
                requestMessage.Content = streamContent;
            }

            // Copy the request headers
            foreach (var header in proxyRequest.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(proxyRequest.Method);

            return requestMessage;
        }

        private void SetHeaders(ref HttpClient client, ref HttpContent content, IHeaderDictionary headers)
        {
            foreach (KeyValuePair<string, StringValues> t in headers)
            {
                if (t.Key.ToLower().Equals("authorization"))
                {
                    var authSections = t.Value.ToString().Split(' ');
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("basic", authSections[1]);
                }
                else if (t.Key.ToLower().Equals("content-type"))
                {
                    string[] values = t.Value.ToString().Split(';');
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(values[0]);
                    for (int i = 1; i < values.Length; i++)
                        content.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue(values[i].Split('=')[0].Trim(), values[i].Split('=')[1]));

                }
                else
                {
                    switch(t.Key.ToLower())
                    {
                        case "accept":
                            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(t.Value.ToString()));
                            break;
                        default:
                                continue;
                    }
                   //if (t.Key.ToLower().Equals("host") || t.Key.ToLower().Equals("accept") || t.Key.ToLower().Equals("user-agent") || t.Key.ToLower().Equals("content-length") || t.Key.ToLower().Equals("connection"))
                    //    continue;
                }

            }
        }

        private string GetContent(Stream content)
        {
            StreamReader reader = new StreamReader(content);
            string data = reader.ReadToEnd();
            return data;
        }   
    }
}
