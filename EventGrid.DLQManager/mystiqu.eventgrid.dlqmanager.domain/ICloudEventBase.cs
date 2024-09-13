
namespace mystiqu.eventgrid.dlqmanager.domain
{
    public interface ICloudEventBase
    {
        object Data { get; set; }
        string DataBase64 { get; set; }

        void LogEventDetails(ref Dictionary<string, object> logMetadata);
    }
}
