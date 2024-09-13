using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Messaging.EventGrid;
using Azure;
using mystiqu.eventgrid.dlqmanager;
using mystiqu.eventgrid.dlqmanager.domain;
using mystiqu.eventgrid.dlqmanager.domain.utilities;




















/*
 * 
    Interface IDLQManager
        List<items> ListBlobs(int maxPages, string ignoreFilter)
        void Publish(List<Items> items)
        void Delete(List<Items> items)

        void Publish(int maxPages, string ignoreFilter, bool deleteBlobWhenPublished) //Do all
*/

internal class EventGridDLQProcessor
{
    public static IConfiguration _configuration;
    private static string _eventGridName = string.Empty;
    private static string _eventGridFolder = string.Empty;
    private static string _eventGridSecret = string.Empty;
    private static string _storageContainer = string.Empty;
    private static string _storageConnectionString = string.Empty;
    private static string _eventGridConnection = string.Empty;
    private static BlobContainerClient _blobContainerClient;

    static EventGridDLQProcessor()
    {
        DateTime t = new DateTime(1970, 1, 1, 0, 0, 0);
        DateTime t2 = new DateTime(2023, 11, 14, 22, 13, 20);

        double sec = t2.Subtract(t).TotalSeconds;


        ServiceCollection serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        IConfigurationSection section = _configuration.GetRequiredSection("Values");
        _eventGridConnection = _configuration.GetConnectionString("EventGridConnectionString");
        _eventGridSecret = section["EventGridSecret"];
        _eventGridName = section["EventGridName"];
        _eventGridFolder = section["EventGridFolder"];
        _storageContainer = section["DeadletterContainer"];
        _storageConnectionString = _configuration.GetConnectionString("StorageConnectionString");

        _blobContainerClient = new BlobContainerClient(_storageConnectionString, _storageContainer);

    }

    public static void ProcessCallback(ProcessingStatus status)
    {
       // Console.WriteLine(status.ToString());
    }

    private static void DLQManagerTest()
    {
        //A set of json properties that are invalid that we do not want to republish
        string[] invalidFilter = new string[] { "\"ReasonCode\": {}", "\"Reason\": {}", "\"ReasonCode\": {}", "\"Reason\":{}" };

        //Create our base folder/prefix
        string _baseFolder = _eventGridName + "/" + _eventGridFolder + "/";

        BlobServiceClient _blobServiceClient = new BlobServiceClient(_storageConnectionString);

        _blobContainerClient = new BlobContainerClient(_storageConnectionString, _storageContainer);
        EventGridPublisherClient eventGridPublisherClient = new EventGridPublisherClient(new Uri(_eventGridConnection), new AzureKeyCredential(_eventGridSecret));

        //Create our DLQ Manager, defining a callback as well
        DLQManager manager = new DLQManager(_blobServiceClient, eventGridPublisherClient, _eventGridName, "dead-letters", _baseFolder, ProcessCallback, new ProcessingStatusOptions() { Ratio = 100, SendCallbackOnError = true});

        //List all available directories, a depdth of 6 gives us <topic>/<handler>/year/month/day/hour
        List<string> directoryNames = manager.ListDeadletterDateFolders(_baseFolder, 6);

        foreach (string directoryName in directoryNames)
            Console.WriteLine($" {directoryName}");

        int blobPageSize = 5000;
        //Iterate over all directories
        foreach (string directoryName in directoryNames)
        {
            //Get blob pages with 5000 blobs per page for a specific directory
            IEnumerable<Page<BlobItem>> blobPages = manager.ListBlobs(directoryName, blobPageSize);
            Console.WriteLine($"Fetched {blobPages.Count()} page(s) for directory {directoryName}");

            //For better user experience, let's fetch 100 items at the time from a given page (just downloading 5000 blobs would take quite some time)
            int pageCount = 0;      //Keep track of the current page number
            int pageSize = 100;     //The page size, i.e. number of blobs to fetch per sub page

            //Iterate over all main blob pages
            foreach (Page<BlobItem> blobPage in blobPages)
            {
                List<DownloadedBlob> downloadedBlobs = new List<DownloadedBlob>();
                pageCount++;
                int internalPageCount = 0;

                //Internal "paging" for each page
                int totalProcessedPerPage = 0;
                do
                {
                    internalPageCount++;

                    //Downloade 100 blobs. Here we specify which blob page, the internal "page" and pagesize so we can get it in small chunks
                    downloadedBlobs = manager.DownloadBlobs(blobPage, internalPageCount, pageSize, invalidFilter);
                    if(downloadedBlobs.Count == 0) {
                        Console.WriteLine($"Found 0 blobs in blob page {pageCount} ({directoryName}),  continuing");
                        continue;
                    }
                    totalProcessedPerPage = (internalPageCount - 1) * pageSize + downloadedBlobs.Count;

                    Console.WriteLine($"Downloaded {totalProcessedPerPage} / {blobPageSize} blobs from blob page {pageCount} ({directoryName})...");

                    string down = DateTime.Now.ToString("HH:mm:ss.ffffff");
                    if (downloadedBlobs.Count > 0)
                    {
                        manager.PublishEvents(downloadedBlobs, false);
                        Console.WriteLine($"Published {totalProcessedPerPage} / {blobPageSize} blobs from blob page {pageCount} ({directoryName})...");

                        manager.DeleteBlobs(downloadedBlobs);
                        Console.WriteLine($"Deleted {totalProcessedPerPage} / {blobPageSize} blobs from blob page {pageCount} ({directoryName})...");
                    }
                } while (downloadedBlobs.Count >= pageSize);
            }
        }
    }

    private static void Welcome()
    {
        string[] invalidFilter = new string[] { "\"ReasonCode\": {}", "\"Reason\": {}", "\"ReasonCode\": {}", "\"Reason\":{}" };

        Console.WriteLine("Welcome to Mike's Event Grid Topic DLQ Reader.");
        Console.WriteLine("");

        Console.WriteLine("This application will list dead-lettered events in a storage account and re-publish the data part to the chosen event grid topic.");
        Console.WriteLine("The following settings was found:");
        Console.WriteLine("  Storage Account: {0}", _blobContainerClient.AccountName);
        Console.WriteLine("  Event Grid Topic: {0}", _eventGridName);
        Console.WriteLine("  Dead-Letter container: {0}", _storageContainer);
        Console.WriteLine("  Storage prefix: {0}", _eventGridFolder);
        Console.WriteLine("");  
    }

    private static void Main(string[] args)
    {
        DLQManagerTest();
    }

    private static string GetBlobName(string blob)
    {
        return blob.Substring(blob.LastIndexOf("/") + 1);
    }
   
    private static void ConfigureServices(IServiceCollection serviceCollection)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        // Build configuration
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
            .AddEnvironmentVariables()
            .AddJsonFile($"appsettings.json", false, true)
            .AddJsonFile($"appsettings.local.json", true, true)
            //.AddJsonFile($"appsettings.{env}.json", true, true)
            .Build();

        // Add access to generic IConfigurationRoot
        //serviceCollection.AddSingleton<IConfigurationRoot>(configuration);

        // Add app
        serviceCollection.AddTransient<EventGridDLQProcessor>();
    }


}