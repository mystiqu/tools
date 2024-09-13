using Newtonsoft.Json;

namespace mystiqu.eventgrid.dlqmanager.domain
{
    public class EventGridDeadLetterEvents
    {
        public EventGridDeadLetterEvents()
        {
            DeadLetterEvents = new List<EventGridDeadLetterEvent>();
        }

        [JsonProperty(PropertyName = "deadLetterEvents")]
        public List<EventGridDeadLetterEvent> DeadLetterEvents { get; set;}
    }
}
