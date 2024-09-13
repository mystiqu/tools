using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Azure.Messaging.EventGrid;
using System.Threading.Tasks;

namespace mystiqu.eventgrid.dlqmanager.domain.utilities
{
    public class CloudEventUtil
    {
        public Azure.Messaging.CloudEvent GetCloudEvent(EventGridDeadLetterEvent evt)
        {
            string data = ((JsonNode)evt.Data).ToJsonString();
            byte[] bData = System.Text.Encoding.UTF8.GetBytes(data);

            FileStream str = System.IO.File.Create($"c:\\temp\\ce\\{evt.Id}.json");
            str.Write(bData, 0, bData.Length);
            str.Close();

            Azure.Messaging.CloudEvent ce = new Azure.Messaging.CloudEvent(evt.Source, evt.Type, evt.Data);
            ce.DataSchema = "";
            ce.DataContentType = "application/json";
            ce.Id = evt.Id;
            ce.Subject = evt.Subject;
            ce.Time = DateTime.Parse(evt.Time);

            return ce;
        }
    }
}
