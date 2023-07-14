using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace al.eventgrid.dlq.domain
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
