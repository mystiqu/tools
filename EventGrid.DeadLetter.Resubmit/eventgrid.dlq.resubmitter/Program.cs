using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using System.Diagnostics;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using al.eventgrid.dlq.domain;
using Azure.Messaging.EventGrid;
using Azure;
using al.eventgrid.dlq.utilities;
using Azure.Messaging;
using System.Runtime.CompilerServices;
using System.Reflection.Metadata;
using static System.Reflection.Metadata.BlobBuilder;
using System.Text.Json.Nodes;

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

    private static void Welcome()
    {
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
        List<EventGridDeadLetterEvent> deadLetterEvents = new List<EventGridDeadLetterEvent>();
        BlobClient blobClient = null;
        string text = string.Empty;
        bool pauseForPermission = true;

        Welcome();

        Console.WriteLine("Looking for dead-letter events in  '{0}/{1}/{2}'", _blobContainerClient.AccountName, _storageContainer, GeneratePrefix());
        Console.Write("Pause before each operation (download, publish, delete) and ask for permission to continue? [Y/N] (default is Y): ");
        ConsoleKeyInfo consoleKeyInfo = new ConsoleKeyInfo('A', ConsoleKey.A, false, false, false);

        consoleKeyInfo = Console.ReadKey(false);
        Console.WriteLine();
        while ((consoleKeyInfo.Key != ConsoleKey.Y && consoleKeyInfo.Key != ConsoleKey.N && consoleKeyInfo.Key != ConsoleKey.Enter))
        {
            Console.WriteLine("");
            Console.Write("Invalid option, try again [Y/N]: ");
            consoleKeyInfo = Console.ReadKey(false);
            Console.WriteLine();
        }

        if (consoleKeyInfo.Key == ConsoleKey.Y || consoleKeyInfo.Key == ConsoleKey.Enter)
        {
            Console.WriteLine("Ask for permission is selected");
            Console.WriteLine();
            pauseForPermission = true;
        }
            
        else
        {
            Console.WriteLine("Skip asking for permission, the process will iterate through all blobs without asking");
            Console.WriteLine();
            pauseForPermission = false;
        }
            
        AskForPermission("Press any key to start downloading events", pauseForPermission);

        IEnumerable<Page<BlobItem>> blobs = _blobContainerClient.GetBlobs(BlobTraits.All, BlobStates.All, GeneratePrefix()).AsPages(default, 100);

        EventGridPublisherClient eventGridPublisherClient = new EventGridPublisherClient(new Uri(_eventGridConnection), new AzureKeyCredential(_eventGridSecret));

        Console.WriteLine("Found {0} blob pages", blobs.Count());
        int i = 1;
        List<string> downloadedBlobs = new List<string>();

        foreach (Page<BlobItem> blobPage in blobs)
        {
            downloadedBlobs.Clear();
            foreach (BlobItem blob in blobPage.Values)
            {
                if (blob.Deleted) {
                    Console.WriteLine("Blob {0} is deleted, continuing...", blob.Name);
                    continue;
                }

                blobClient = _blobContainerClient.GetBlobClient(blob.Name);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    try
                    {
                        Console.WriteLine("Downloading {0} from page {1}", blob.Name, i);

                        blobClient.DownloadTo(memoryStream);
                        text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());

                        List<EventGridDeadLetterEvent> evts = Transform.TransformJsonStringToEventGridDeadLetterEvents(text, blob.Name);
                        deadLetterEvents.AddRange(evts);
                        downloadedBlobs.Add(blobClient.Name);
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }


                }
            }

            Console.WriteLine("Found and downloadad {0} dead-lettered events for page {1}", deadLetterEvents.Count(), i);
            AskForPermission($"Press any key to re-publish them to {_eventGridName}", pauseForPermission);

            foreach (EventGridDeadLetterEvent evt in deadLetterEvents)
            {

                Console.WriteLine("Publishing {0}, {1}... ", evt.Subject, evt.Id);
                Azure.Messaging.CloudEvent ce = GetCloudEvent(evt);

                Response t = eventGridPublisherClient.SendEvent(ce);
                if (t.IsError)
                {
                    Console.WriteLine("  Failed to publish {0}, {1} ", ce.Id, t.ReasonPhrase);
                    Console.ReadKey();
                }
            }

            AskForPermission("Press any keys to delete blobs for this pages", pauseForPermission);

            Console.WriteLine("Deleting blobs for page {0}", blobPage.ContinuationToken != null ? blobPage.ContinuationToken : "");

            for (int j = 0; j < downloadedBlobs.Count; j++)
            {
                //BlobItem currentItem = blobPage.Values[j];
                Console.WriteLine("  Deleting {0}...", downloadedBlobs[j]);
                blobClient = _blobContainerClient.GetBlobClient(downloadedBlobs[j]);
                if (blobClient.Exists())
                {
                    Response r = blobClient.Delete(DeleteSnapshotsOption.IncludeSnapshots);
                    if (r.IsError)
                    {
                        Console.WriteLine("  Failed to delete {0}, {1} ", downloadedBlobs[j], r.ReasonPhrase);
                        Console.ReadKey();
                    }
                }

            }

            AskForPermission($"Successfully re-published {deadLetterEvents.Count()} events to {_eventGridName}. Press any key to continue", pauseForPermission);

            deadLetterEvents.Clear();
            i++;
        }
    }

    private static void AskForPermission(string text, bool pauseForPermission)
    {
        if (pauseForPermission)
        {
            Console.WriteLine("Press any key to start downloading events");
            Console.ReadKey();
        }
    }

    private static Azure.Messaging.CloudEvent GetCloudEvent(EventGridDeadLetterEvent evt)
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

    private static string GeneratePrefix()
    {
        return _eventGridName + "/" + _eventGridFolder;
    }

    private static void ConfigureServices(IServiceCollection serviceCollection)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        // Build configuration
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
            .AddEnvironmentVariables()
            .AddJsonFile($"appsettings.json", false, true)
            //.AddJsonFile($"appsettings.{env}.json", true, true)
            .Build();

        // Add access to generic IConfigurationRoot
        //serviceCollection.AddSingleton<IConfigurationRoot>(configuration);

        // Add app
        serviceCollection.AddTransient<EventGridDLQProcessor>();
    }


}