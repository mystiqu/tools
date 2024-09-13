using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;

namespace mystiqu.eventgrid.dlqmanager.domain.utilities
{
    public static class Transform
    {
        public static EventGridDeadLetterEvent TransformJsonStringToEventGridDeadLetterEvent(string json)
        {
            JsonNode jsonNode = JsonObject.Parse(json);

            //EventGridDeadLetterEvent dlqEvent = JsonConvert.DeserializeObject<EventGridDeadLetterEvent>(json);

            //return dlqEvent;
            EventGridDeadLetterEvent dlqEvent = new domain.EventGridDeadLetterEvent()
            {
                deadletterreason = jsonNode["deadletterreason"].ToString(),
                deliveryattempts = int.Parse(jsonNode["deliveryattempts"].ToString()),
                lastdeliveryoutcome = jsonNode["lastdeliveryoutcome"].ToString(),
                lasthttpstatuscode = int.Parse(jsonNode["lasthttpstatuscode"].ToString()),
                publishtime = DateTime.Parse(jsonNode["publishtime"].ToString()),
                lastdeliveryattempttime = DateTime.Parse(jsonNode["lastdeliveryattempttime"].ToString()),
                Id = jsonNode["id"].ToString(),
                Data = jsonNode["data"],
                Source = jsonNode["source"].ToString(),
                Time = jsonNode["time"].ToString(),
                Subject = jsonNode["subject"].ToString(),
                Type = jsonNode["type"].ToString(),
                Specversion = jsonNode["specversion"].ToString(),
                Datacontenttype = jsonNode["datacontenttype"].ToString(),
                DataSchema = jsonNode["dataschema"] != null ? jsonNode["dataschema"].ToString() : string.Empty,
                DataBase64 = jsonNode["data_base64"] != null ? jsonNode["data_base64"].ToString() : string.Empty

            };

            return dlqEvent;
        }

        public static EventGridDeadLetterEvent TransformJsonStringToEventGridDeadLetterEvent(string json, string blobName)
        {
            EventGridDeadLetterEvent evt = TransformJsonStringToEventGridDeadLetterEvent(json);
            evt.blobname = blobName;

            return evt;
        }

        public static List<EventGridDeadLetterEvent> TransformJsonStringToEventGridDeadLetterEvents(string json)
        {
            List<EventGridDeadLetterEvent> events = new List<EventGridDeadLetterEvent>();
            JsonArray jsonNodes = (JsonArray)JsonObject.Parse(json);
            foreach (JsonNode node in jsonNodes)
                events.Add(TransformJsonStringToEventGridDeadLetterEvent(node.ToJsonString()));

            return events;
        }

        public static List<EventGridDeadLetterEvent> TransformJsonStringToEventGridDeadLetterEvents(string json, string blobName)
        {
            List<EventGridDeadLetterEvent> events = new List<EventGridDeadLetterEvent>();
            JsonArray jsonNodes = (JsonArray)JsonObject.Parse(json);
            foreach (JsonNode node in jsonNodes)
                events.Add(TransformJsonStringToEventGridDeadLetterEvent(node.ToJsonString(), blobName));

            return events;
        }
    }

    public class RawJsonConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return JObject.Load(reader).ToString();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue((string)value);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}
