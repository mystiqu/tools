using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;

namespace mystiqu.eventgrid.dlqmanager.domain
{ 

    public class DownloadedBlob 
    {
        public DownloadedBlob()
        {
            Payload = string.Empty;
            FileName = string.Empty;
            Error = string.Empty;
        }

        public string Payload { get; set; }
        public string FileName { get; set; }
        public string Error { get; set; }


    }
}
