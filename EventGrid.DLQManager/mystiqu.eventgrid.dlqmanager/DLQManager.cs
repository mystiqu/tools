using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Messaging.EventGrid;
using Azure;
using Azure.Identity;
using System.Text.Json.Nodes;
using mystiqu.eventgrid.dlqmanager.domain;
using mystiqu.eventgrid.dlqmanager.domain.utilities;
using Azure.Messaging.ServiceBus;
using mystiqu.eventgrid.dlqmanager;

namespace mystiqu.eventgrid.dlqmanager;

public class DLQManager : IDLQManager
{
    private string _blobConnectionString;
    private string _eventGridTopicName;
    private string _eventGridConnectionString;
    private string _eventgridKey;
    private string _deadlettercontainer;
    private string _baseFolder;                                     //deadLettercontainer/baseFolder
    private string _azureIdentityId;                                //Managed identity id
    private DefaultAzureCredential _azureCredential;                //For managed identity
    //private ManagedIdentityCredential _managedIdentityCredential;
    private BlobServiceClient _blobServiceClient;
    private BlobContainerClient _blobContainerClient;
    private EventGridPublisherClient _eventGridPublisherClient;
    public delegate void ProcessingStatusCallback(ProcessingStatus status);
    private ProcessingStatusCallback _processingCallback;
    ProcessingStatusOptions _processingStatusOptions;

    /// <summary>
    /// Constructor for Managed Identity, unless eventgridKey is specifically used and azureIdentityId is null
    /// </summary>
    /// <param name="blobConnectionString">Blob connection string</param>
    /// <param name="eventGridConnectionString">Eventgrid connection string</param>
    /// <param name="eventgridKey">Eventgrid SAS key. If azureIdentityId is supplied, it's used over sas key</param>
    /// <param name="eventGridTopicName">Eventgrid topic name</param>
    /// <param name="deadletterContainer">Deadletter container name, e.g. dead-letters</param>
    /// <param name="baseFolder">Base prefix to use when searching for blobs and listing folders</param>
    /// <param name="azureIdentityId">Identity id (managed identity)</param>
    /// <param name="processingCallback">Optional callback for continous processing status</param>
    /// <param name="processingStatusOptions">Optional processing options in case of using callback</param>
    public DLQManager(string blobConnectionString, string eventGridConnectionString, string eventgridKey, string eventGridTopicName, string deadletterContainer, string baseFolder, string azureIdentityId, ProcessingStatusCallback processingCallback, ProcessingStatusOptions processingStatusOptions)
    {
        _eventgridKey = string.Empty;
        _eventGridTopicName = eventGridTopicName;
        _deadlettercontainer = deadletterContainer;
        _baseFolder = baseFolder;
        _azureIdentityId = azureIdentityId;
        _blobConnectionString = blobConnectionString;
        _eventGridConnectionString = eventGridConnectionString;
        _processingCallback = processingCallback;
        _processingStatusOptions = processingStatusOptions;

        if (!string.IsNullOrEmpty(_azureIdentityId))
        {
            _azureCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions()
            {
                ManagedIdentityClientId = _azureIdentityId
            });
        }
        else
            _azureCredential = null!;

        //If in debug mode, just use plain connection strings
        //If not, check the _azureCredential and use that as the first options - otherwizse sas keys
#if DEBUG
        _blobServiceClient = new BlobServiceClient(_blobConnectionString);
        _eventGridPublisherClient = new EventGridPublisherClient(new Uri(_eventGridConnectionString), new AzureKeyCredential(_eventgridKey));
#else
            if(_azureCredential != null) {
                _eventGridPublisherClient = new EventGridPublisherClient(new Uri(_eventGridConnectionString), _azureCredential);
                _blobServiceClient = new BlobServiceClient(new Uri(_blobConnectionString), _azureCredential);
            }
            else 
            {
                _blobServiceClient = new BlobServiceClient(_blobConnectionString);
                _eventGridPublisherClient = new EventGridPublisherClient(new Uri(_eventGridConnectionString), new AzureKeyCredential(_eventgridKey));
            }
#endif

        _blobContainerClient = _blobServiceClient.GetBlobContainerClient(_deadlettercontainer);

    }

    /// <summary>
    /// Constructor where you inject the necessary instances for full controll over access managagement
    /// </summary>
    /// <param name="blobServiceClient"></param>
    /// <param name="eventGridPublisherClient"></param>
    /// <param name="eventGridTopicName">Eventgrid topic name</param>
    /// <param name="deadletterContainer">Deadletter container name, e.g. dead-letters</param>
    /// <param name="baseFolder">Base prefix to use when searching for blobs and listing folders</param>
    /// <param name="processingCallback">Optional callback for continous processing status</param>
    /// <param name="processingStatusOptions">Optional processing options in case of using callback</param>
    public DLQManager(BlobServiceClient blobServiceClient, EventGridPublisherClient eventGridPublisherClient, string eventGridTopicName, string deadletterContainer, string baseFolder, ProcessingStatusCallback processingCallback, ProcessingStatusOptions processingStatusOptions)
    {
        _blobConnectionString = string.Empty;
        _eventGridConnectionString = string.Empty;
        _eventgridKey = string.Empty;
        _azureIdentityId = string.Empty;
        _azureCredential = null!;

        _eventGridTopicName = eventGridTopicName;
        _deadlettercontainer = deadletterContainer;
        _baseFolder = baseFolder;
        _processingCallback = processingCallback;
        _processingStatusOptions = processingStatusOptions;
        _blobServiceClient = blobServiceClient;

        _blobContainerClient = _blobServiceClient.GetBlobContainerClient(_deadlettercontainer);
        _eventGridPublisherClient = eventGridPublisherClient;
    }

    /// <summary>
    /// List all folders which we can iterate over later. Recursive function
    /// </summary>
    /// <param name="_baseFolder">The base folder to start from, e.g. {topicname}/{handlername}</handlerN></topicname></param>
    /// <param name="directoryNames">List of directories found, populated during recursive calls</param>
    /// <param name="maxDepth">Number of levels to traverse. E.g. /topic/handler/year/month/day/hour means 6 levels</param>
    /// <returns></returns>
    public List<string> ListDeadletterDateFolders(string _baseFolder, int maxDepth)
    {
        List<string> directoryNames = new List<string>();

        ListDeadletterDateFolders(_baseFolder, directoryNames, maxDepth);

        SendCallback(PROCESS_TYPE.LIST, directoryNames.Count);

        return directoryNames;
    }

    private void ListDeadletterDateFolders(string _baseFolder, List<string> directoryNames, int maxDepth)
    {
        if (directoryNames == null)
            directoryNames = new List<string>();

        Pageable<BlobHierarchyItem> items = _blobContainerClient.GetBlobsByHierarchy(BlobTraits.None, BlobStates.None, "/", _baseFolder);

        foreach (BlobHierarchyItem blobHierarchyItem in items)
        {
            if (blobHierarchyItem.IsPrefix && blobHierarchyItem.Prefix.TrimEnd('/').Split("/").Count() < maxDepth)
            {
                ListDeadletterDateFolders(blobHierarchyItem.Prefix, directoryNames, maxDepth);
            }
            else
            {
                if (!directoryNames.Exists(x => x == blobHierarchyItem.Prefix))
                    directoryNames.Add(blobHierarchyItem.Prefix);
            }
        }
    }

    /// <summary>
    /// List all directories with a given prefix
    /// </summary>
    /// <param name="prefix">Prefix, e.g {topicname}/{handler}</param>
    /// <param name="maxPages">Number of pages to fetch as a maximum. Note: does not need to be honored by the SDK.</param>
    /// <returns>List of blob pages</returns>
    public IEnumerable<Page<BlobItem>> ListBlobs(string prefix, int pageSize)
    {
        return _blobContainerClient.GetBlobs(BlobTraits.None, BlobStates.None, prefix).AsPages(default, pageSize);
    }

    /// <summary>
    /// Download all blobs in a given blob page
    /// </summary>
    /// <param name="blobPage">Blob page</param>
    /// <param name="page">The page within the blob page, for proper paging</param>
    /// <param name="blobPage">Page size, used for paging - e.g. fetch 100 items at a time</param>
    /// <param name="invalidFilters">If a blob payload contain any of these values, the blob is ignored</param>
    /// <returns>List of downloaded blobs</returns>
    public List<DownloadedBlob> DownloadBlobs(Page<BlobItem> blobPage, int page, int pageSize, string[] invalidFilters)
    {
        List<DownloadedBlob> downloadedBlobs = new List<DownloadedBlob>();
        BlobClient blobClient;
        string payload = string.Empty;
        int count = 0;
        if (page == 0)
            page = 1;

        foreach (BlobItem blob in blobPage.Values.Skip(page-- * pageSize))
        {
            count++;

            DownloadedBlob downloadedBlob = new DownloadedBlob();
            if (blob.Deleted)
            {
                continue;
            }

            blobClient = _blobContainerClient.GetBlobClient(blob.Name);
            downloadedBlob = DownloadBlob(blob, blobClient, invalidFilters);
            downloadedBlobs.Add(downloadedBlob);

            SendCallback(PROCESS_TYPE.DOWNLOAD, blob.Name, null);

            if (count == pageSize)
                break;
        }

        return downloadedBlobs;
    }

    /// <summary>
    /// Download all blobs in a given blob page
    /// </summary>
    /// <param name="blobPage">Blob page</param>
    /// <param name="page">The page within the blob page, for proper paging</param>
    /// <param name="blobPage">Page size, used for paging - e.g. fetch 100 items at a time</param>
    /// <param name="directory">Save the downloaded blobs to this directory</param>
    /// <param name="invalidFilters">If a blob payload contain any of these values, the blob is ignored</param>
    /// <returns>List of downloaded blobs</returns>
    public List<DownloadedBlob> DownloadBlobs(Page<BlobItem> blobPage, int page, int pageSize, string directory, string[] invalidFilters)
    {
        List<DownloadedBlob> downloadedBlobs = new List<DownloadedBlob>();
        BlobClient blobClient;
        string payload = string.Empty;
        int count = 0;
        if (page == 0)
            page = 1;

        foreach (BlobItem blob in blobPage.Values.Skip(page-- * pageSize))
        {
            count++;

            DownloadedBlob downloadedBlob = new DownloadedBlob();
            if (blob.Deleted)
            {
                continue;
            }

            blobClient = _blobContainerClient.GetBlobClient(blob.Name);
            downloadedBlob = DownloadBlob(blob, blobClient, invalidFilters);
            downloadedBlobs.Add(downloadedBlob);

            SaveBlobToFile(payload, blob.Name, directory);

            SendCallback(PROCESS_TYPE.DOWNLOAD, blob.Name, null);

            if (count == pageSize)
                break;
        }

        return downloadedBlobs;
    }

    private void SaveBlobToFile(string payload, string blobFullName, string directory)
    {
        directory = directory.TrimEnd('/');
        string file = directory + "/" + GetBlobName(blobFullName);

        FileStream str = System.IO.File.Create(file);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(payload);
        str.Write(data, 0, data.Length);
        str.Close();
    }

    private string GetBlobName(string blobFullName)
    {
        return blobFullName.Substring(blobFullName.LastIndexOf('/') + 1);
    }

    /// <summary>
    /// Published preciously downloaded blobs to the configured eventgrid topic
    /// </summary>
    /// <param name="blobs"  cref="DownloadedBlob">List of downloaded blobs</param>
    /// <param name="deleteBlobs">Delete blobs after successfull publish</param>
    /// <returns cref="DownloadedBlob">List of EventGridDeadLetterEvent's that was republished</returns>
    /// <exception cref="Exception"></exception>
    public List<EventGridDeadLetterEvent> PublishEvents(List<DownloadedBlob> blobs, bool deleteBlobs = false)
    {
        int count = 0;
        List<EventGridDeadLetterEvent> deadLetterEvents = new List<EventGridDeadLetterEvent>();

        //Create and transform to dlq events
        foreach (DownloadedBlob blob in blobs)
        {
            List<EventGridDeadLetterEvent> evts = Transform.TransformJsonStringToEventGridDeadLetterEvents(blob.Payload, blob.FileName);
            deadLetterEvents.AddRange(evts);
        }

        foreach (EventGridDeadLetterEvent evt in deadLetterEvents)
        {
            count++;
            Azure.Messaging.CloudEvent ce = GetCloudEvent(evt);

            Response t = _eventGridPublisherClient.SendEvent(ce);
            if (t.IsError)
            {
                SendCallback(PROCESS_TYPE.PUBLISH, evt.blobname, new Exception(t.ReasonPhrase));
                throw new Exception(t.ReasonPhrase);
            }

            SendCallback(PROCESS_TYPE.PUBLISH, evt.blobname, null);
        }

        if (deleteBlobs)
        {
            DeleteBlobs(blobs);
        }

        return deadLetterEvents;
    }

    public List<ServiceBusMessage> PublishToServiceBus(List<DownloadedBlob> blobs, string serviceBusQueue, bool deleteBlobs = false)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Deletes a list of downloaded blobs from storage
    /// </summary>
    /// <param name="downloadedBlobs" cref="DownloadedBlob">Blob to delete</param>
    /// <returns>List of blob names that was deleted</returns>
    public List<string> DeleteBlobs(List<DownloadedBlob> downloadedBlobs)
    {
        BlobClient blobClient;
        List<string> deletedBlobs = new List<string>();
        int count = 0;

        foreach (DownloadedBlob blob in downloadedBlobs)
        {
            count++;
            blobClient = _blobContainerClient.GetBlobClient(blob.FileName);
            if (blobClient.Exists())
            {
                Response r = blobClient.Delete(DeleteSnapshotsOption.IncludeSnapshots);
                if (r.IsError)
                {
                    SendCallback(PROCESS_TYPE.DOWNLOAD, blob.FileName, new Exception(r.ReasonPhrase));
                    //throw new Exception(r.ReasonPhrase);
                }

                SendCallback(PROCESS_TYPE.DELETE, blob.FileName, null);
                deletedBlobs.Add(blob.FileName);
            }
        }

        return deletedBlobs;
    }

    private void SendCallback(int ratio, int count, string lastBlobName, PROCESS_TYPE processType)
    {
        if (ratio % 100 == 0)
        {
            ProcessingStatus s = new ProcessingStatus()
            {
                Count = count,
                ProcessType = processType,
                Name = lastBlobName
            };

            if (_processingCallback != null)
                _processingCallback(s);
        }
    }

    private void SendCallback(PROCESS_TYPE processType, string name, Exception ex)
    {
        ProcessingStatus s = new ProcessingStatus()
        {
            ProcessType = processType,
            Name = name,
            Exception = ex
        };

        if (_processingCallback != null)
            _processingCallback(s);
    }

    private void SendCallback(PROCESS_TYPE processType, int count)
    {
        ProcessingStatus s = new ProcessingStatus()
        {
            ProcessType = processType,
            Count = count
        };

        if (_processingCallback != null)
            _processingCallback(s);
    }

    private Azure.Messaging.CloudEvent GetCloudEvent(EventGridDeadLetterEvent evt)
    {
        string data = ((JsonNode)evt.Data).ToJsonString();
        byte[] bData = System.Text.Encoding.UTF8.GetBytes(data);

        Azure.Messaging.CloudEvent ce = new Azure.Messaging.CloudEvent(evt.Source, evt.Type, evt.Data);
        ce.DataSchema = "";
        ce.DataContentType = "application/json";
        ce.Id = evt.Id;
        ce.Subject = evt.Subject;
        ce.Time = DateTime.Parse(evt.Time);

        return ce;
    }

    private DownloadedBlob DownloadBlob(BlobItem blob, BlobClient blobClient, string[] invalidFilters)
    {
        DownloadedBlob downloadedBlob = new DownloadedBlob();
        string payload = string.Empty;

        using (MemoryStream memoryStream = new MemoryStream())
        {
            try
            {
                blobClient.DownloadTo(memoryStream);
                payload = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                downloadedBlob.Payload = payload;
                downloadedBlob.FileName = blob.Name;

                //Check for crappy json
                for (int j = 0; j < invalidFilters.Length; j++)
                {
                    if (payload.Contains(invalidFilters[j]))
                    {
                        downloadedBlob.Error = "Invalid json, deleting blob directly";
                        blobClient = _blobContainerClient.GetBlobClient(blob.Name);

                        try
                        {
                            if (blobClient.Exists())
                            {
                                Response r = blobClient.Delete(DeleteSnapshotsOption.IncludeSnapshots);
                                if (r.IsError)
                                {
                                }
                            }
                        }
                        catch (Exception failedEx)
                        {
                            SendCallback(PROCESS_TYPE.DOWNLOAD, blob.Name, failedEx);
                            downloadedBlob.Error += Environment.NewLine;
                            downloadedBlob.Error += failedEx.ToString();
                        }

                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                SendCallback(PROCESS_TYPE.DOWNLOAD, blob.Name, ex);
            }
        }

        return downloadedBlob;
    }



}
