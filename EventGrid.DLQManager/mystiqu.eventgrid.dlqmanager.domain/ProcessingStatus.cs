using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;

namespace mystiqu.eventgrid.dlqmanager.domain
{ 
    public enum PROCESS_TYPE
    {
        LIST  =0,
        DOWNLOAD = 1,
        PUBLISH = 2,
        DELETE = 3
    }

    public class ProcessingStatus 
    {
        public ProcessingStatus()
        {
            ProcessType = PROCESS_TYPE.DOWNLOAD;
            Count = 0;
            Exception = null!;
            Name = string.Empty;
            Page = 0;
        }

        public PROCESS_TYPE ProcessType { get; set; }
        public int Count { get; set; }
        public int Page { get; set; }
        public string Name { get; set; } //If using for each individual blob
        public Exception Exception { get; set; }

        public override string ToString()
        {
            if (ProcessType == PROCESS_TYPE.LIST)
                return $"Listed {Count} folders";

            if (Count == 0)
            {
                if (ProcessType == PROCESS_TYPE.DOWNLOAD)
                {
                    if (Exception == null)
                        return $"Downloading {Name}" + GetPageText();
                    else
                        return $"Error downloading {Name}. {Exception.ToString()}" + GetPageText();
                }

                if (ProcessType == PROCESS_TYPE.PUBLISH)
                {
                    if (Exception == null)
                        return $"Publisging {Name}" + GetPageText();
                    else
                        return $"Error publishing {Name}. {Exception.ToString()}" + GetPageText(); ;
                }

                if (ProcessType == PROCESS_TYPE.DELETE)
                {
                    if (Exception == null)
                        return $"Deleting {Name}" + GetPageText();
                    else
                        return $"Error deleting {Name}. {Exception.ToString()}" + GetPageText();
                }
            }
            else
            {
                if (ProcessType == PROCESS_TYPE.DOWNLOAD)
                {
                    if (Exception == null)
                        return $"Downloaded {Count.ToString().PadLeft(3, '0')} blobs, last blob: {Name}";
                    else
                        return $"Error downloading {Name}. {Exception.ToString()}";
                }

                if (ProcessType == PROCESS_TYPE.PUBLISH)
                {
                    if (Exception == null)
                        return $"Published {Count.ToString().PadLeft(3, '0')} events, last event: {Name}";
                    else
                        return $"Error publishing {Name}. {Exception.ToString()}";
                }

                if (ProcessType == PROCESS_TYPE.DELETE)
                {
                    if (Exception == null)
                        return $"Deleted {Count.ToString().PadLeft(3, '0')} blobs, last blob: {Name}";
                    else
                        return $"Error deleting {Name}. {Exception.ToString()}";
                }
            }

            return Count.ToString() + ": " + Name;

        }

        private string GetPageText()
        {
            return Page > 0 ? $" from page {Page.ToString().PadLeft(3, '0')}" : "";
        }
    }

    public class ProcessingStatusOptions
    {
        public ProcessingStatusOptions()
        {
            Ratio = 100;
            SendCallbackOnError = true;
        }

        /// <summary>
        /// How many ProcessingStatus events would you like to reive. 1 for each record, 100 for every 100 record processed
        /// </summary>
        public int Ratio { get; set; }
        public bool SendCallbackOnError { get; set; }
    }
}
