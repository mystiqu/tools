using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace al.eventgrid.dlq.domain
{ 

    public class CloudEvent : CloudEventBase, ICloudEventBase
    {
        public CloudEvent()
        {
            DataBase64 = string.Empty;
        }

        [JsonProperty("data")]
        public object Data { get; set; }

        [JsonProperty("data_base64")]
        public string DataBase64 { get; set; }

        public new void LogEventDetails(ref Dictionary<string, object> logMetadata)
        {
            base.LogEventDetails(ref logMetadata);
        }
    }
}
