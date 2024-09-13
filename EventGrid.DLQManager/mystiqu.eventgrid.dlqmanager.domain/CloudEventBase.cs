using Newtonsoft.Json;

namespace mystiqu.eventgrid.dlqmanager.domain
{
    public class CloudEventBase
    {
        public CloudEventBase()
        {
            Id = string.Empty;
            Specversion = string.Empty;
            Type = string.Empty;
            Source = string.Empty;
            Subject = string.Empty;
            Time = string.Empty;
            Datacontenttype = string.Empty;
            DataSchema = string.Empty;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("specversion")]
        public string Specversion { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("datacontenttype")]
        public string Datacontenttype { get; set; }

        [JsonProperty("data-schema")]
        public string DataSchema { get; set; }

        public void LogEventDetails(ref Dictionary<string, object> logMetadata)
        {
            logMetadata["cloudeventtype"] = this.Type;
            logMetadata["cloudeventid"] = this.Id;
            logMetadata["cloudeventsource"] = this.Source;
            logMetadata["cloudeventsubject"] = this.Subject;
            logMetadata["cloudeventtime"] = this.Time;
            logMetadata["cloudeventdatacontenttype"] = this.Datacontenttype;
            logMetadata["cloudeventdataschema"] = this.DataSchema;
            logMetadata["cloudeventspecversion"] = this.Specversion;
        }
    }
}
