using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace console.input.domain
{
    public enum PROPERTY_TYPE
    {
        KEY_VALUE = 1,
        KEY_ONLY = 2,
        UNKOWN = 0
    }

    [JsonObject]
    public class InputSchema
    {
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "propertyprefix")]
        public char PropertyPrefix { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public List<InputProperty> Properties { get; set; }

        [JsonProperty(PropertyName = "defaultschema")]
        public bool DefaultSchema { get { return Properties.Count() == 0; } }

        public InputSchema()
        {
            Properties = new List<InputProperty>();
            Description = "";
            PropertyPrefix = '-';
        }
        
    }

    [JsonObject]
    public class InputProperty
    {
        [JsonProperty(PropertyName = "key")]
        public string Key { get; set; }

        [JsonProperty(PropertyName = "type")]
        public PROPERTY_TYPE Type { get; set; }  

        public bool Required { get; set; }
        
        public string HelpText { get; set;
        }
        public InputProperty()
        {
            Key = "";
            Type = PROPERTY_TYPE.UNKOWN;
            Required = false;
            HelpText = "";
        }
    }
}
