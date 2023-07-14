using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace al.eventgrid.dlq.domain
{
    public interface ICloudEventBase
    {
        object Data { get; set; }
        string DataBase64 { get; set; }

        void LogEventDetails(ref Dictionary<string, object> logMetadata);
    }
}
