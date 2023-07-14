using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace al.eventgrid.dlq.domain
{
    public class EventGridDeadLetterEvent : CloudEvent
    {
        [JsonProperty(PropertyName = "blobname")]
        public string blobname { get; set; }

        [JsonProperty(PropertyName = "deadletterreason")]
        public string deadletterreason { get; set; }

        [JsonProperty(PropertyName = "deliveryattempts")]
        public int deliveryattempts { get; set; }

        [JsonProperty(PropertyName = "lastdeliveryoutcome")]
        public string lastdeliveryoutcome { get; set; }

        [JsonProperty(PropertyName = "lasthttpstatuscode")]
        public int lasthttpstatuscode { get; set; }

        [JsonProperty(PropertyName = "publishtime")]
        public DateTime publishtime { get; set; }

        [JsonProperty(PropertyName = "lastdeliveryattempttime")]
        public DateTime lastdeliveryattempttime { get; set; }

    }
}
