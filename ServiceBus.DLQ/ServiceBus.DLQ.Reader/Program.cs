using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Collections.Generic;
using ConsoleSBReader.factories;
using ConsoleSBReader.parsers;
using Microsoft.AspNetCore.Http;

namespace ConsoleSBReader
{
    public class SBConsoleReader
    {
        public static IConfiguration configuration;
        private static string _serviceBusConnectionString;
        private static string _parserName;
        private static IParser _parser;
        private static string _storageConnectionString;
        static ServiceBusClient _client;
        static CloudBlobContainer _storageContainer;

        private static CloudStorageAccount _storageAccount = null;
        static ServiceBusProcessor processor;

        private static string _queue;
        private static bool _clearDlq = false;
        private static string _container;
        private static string _base64encodeDecode;
        private static string _filePath;
        private static bool _useFileStorage = true; 
        private static bool _isInteractive = false;
        private static int counter=0;
        private static int prefetchCount = 0;
        private static readonly object lockObject = new object();
        private static bool  _emulatorMode = true;

        private static ACTION _actionBeforeWrite = ACTION.NOTHING;

        static SBConsoleReader()
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceBusConnectionString = configuration.GetConnectionString("ServiceBusConnection");
            _storageConnectionString = configuration.GetConnectionString("StorageConnection");
        }

        // handle received messages
        static async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();

            IDictionary<string, string> dic = new Dictionary<string, string>();
            foreach (KeyValuePair<string, object> vp in args.Message.ApplicationProperties)
                dic.Add(new KeyValuePair<string, string>(vp.Key, vp.Value.ToString()));

            string Counter = GetIncreaseCounter().ToString().PadLeft(6, '0');
            Console.WriteLine($"Received [{Counter}]: {args.Message.MessageId}");

            //Save the file
            await Savefile(args.Message.EnqueuedTime.ToString("yyyyMMddTHHmmss") + "-" + args.Message.MessageId + ".txt", body, dic);
            Console.WriteLine($"Saved [{Counter}]: {args.Message.MessageId}");
            // complete the message. messages is deleted from the queue. 

            if (!_emulatorMode)
                await args.CompleteMessageAsync(args.Message);
        }
            
        private static int GetIncreaseCounter()
        {
            lock (lockObject)
            {
                counter++;
            }

            return counter;
        }

        private static async Task Savefile(string filename, string content, IDictionary<string, string> dic)
        {
            if(_useFileStorage)
            {
                await SaveLocalFile(filename, content, dic);
            }
            else
            {
                await SaveFileToStorage(filename, content, dic);
            }
        }

        private static async Task SaveLocalFile(string filename, string content, IDictionary<string, string> dic)
        {
            StreamWriter writer = new StreamWriter(GetCompleteFilePath(filename) + ".txt");

            if (_parser != null)
                content = _parser.Parse(content);

            byte[] data = System.Text.Encoding.UTF8.GetBytes(content);
            char[] chars = System.Text.Encoding.UTF8.GetChars(data);

            await writer.WriteAsync(chars, 0, chars.Length);

            writer.Flush();
            writer.Close();
        }

        private static string GetCompleteFilePath(string filename)
        {
            if (_filePath.EndsWith("\\"))
                return _filePath + filename;
            else
                return _filePath + "\\" + filename;
        }

        private static async Task SaveFileToStorage(string fileName, string content, IDictionary<string, string> dic)
        {
            if (_parser != null)
                content = _parser.Parse(content);

             await _storageContainer.GetBlockBlobReference(fileName + ".txt").UploadTextAsync(content);

            foreach (KeyValuePair<string, string> vp in dic)
                _storageContainer.GetBlockBlobReference(fileName + ".txt").Metadata.Add(vp.Key, vp.Value);

            _storageContainer.GetBlockBlobReference(fileName + ".txt").SetMetadata();
        }

        // handle any errors when receiving messages
        static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        public static async Task Main(string[] args)
        {
            try
            {
                ReadInputParameters(args);

                if (!_clearDlq)
                    await ReadFromQueue();
                else
                    ClearQueueOptions();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
            finally
            {
                if(processor != null)
                    await processor.DisposeAsync();

                if(_client != null)
                    await _client.DisposeAsync();
            }
        }

        private static void ReadInputParameters(string[] args)
        {
            Welcome();
            ParseParameters(args);

            if (_isInteractive)
            {
                InteractiveInput();
            }
        }

        private static async Task ClearQueueOptions()
        {
            string result = ReadInput($"Purge mode selected, select what to purge: Q (queue), D (DLQ): ", new[] { "Q", "D" }, false);
            string queuePrint = _queue;

            if(result.ToLower() == "q")
            {
                _clearDlq = false;
                Console.WriteLine("You have seleced to clear the queue");
                result = ReadInput($"Are you 100% sure you want to clear '{_queue}' of all messages? [N/Y] (Default is N): ", new[] { "Y", "N" }, "N", true);
            }
            else 
            {
                _clearDlq = true;
                queuePrint = _queue + " DLQ";
                Console.WriteLine("You have seleced to clear the dead-letter queue");
                result = ReadInput($"Are you 100% sure you want to clear '{_queue}' of all messages? [N/Y] (Default is N): ", new[] { "Y", "N" }, "N", false);
            }

            if(result.ToLower() == "n")
            {
                Console.WriteLine("You changed your mind - that's also cool... press any key to quit");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("You're brave and I guess you know what your doing");
            Console.WriteLine($"Press any key to start clearing '{_queue}' ");
            Console.ReadKey();

            CreateSB();
            await CreateClearQueueProcessor();
            await processor.StartProcessingAsync();

            //To stop!
            Console.ReadKey();

            Write("Stopping the receiver... ", false);
            await processor.StopProcessingAsync();
            Write("stopped", true);
            Write("", true);
            Write("Press any key to quit!", true);
            Console.ReadKey();

        }

        private static async Task ReadFromQueue()
        {
            CreateParser();
            CreateSB();
            CreateStorage();

            Write($"Press any key to start reading message from {_queue} DLQ", true);
            Write($"Remember, you can press any key to stop processing", true);
            Write("", true);
            Console.ReadKey();
            await CreateSBProcessor(_queue);
           
            //To stop!
            Console.ReadKey();

            Write("Stopping the receiver... ", false);
            await processor.StopProcessingAsync();
            Write("stopped", true);
            Write("", true);
            Write("Press any key to quit!", true);
            Console.ReadKey();
        }

        private static void CreateParser()
        {
            _parser = ContentFactory.GetParser(_parserName, _actionBeforeWrite);
        }

        private static void CreateStorage()
        {
            if (!_useFileStorage)
                CreateBlobStorage(_container);
            else
                CreateFileStorage(_filePath);
        }

        private static void InteractiveInput()
        {
            Write("Interactive mode selected", true);
            _clearDlq = ReaBooleanInput("Clear DLQ? [false] <enter to skip>: ", new string[] {"true", "false"}, false, true);
            _queue = ReadInput("Queue name: ");
            if (_clearDlq)
                return;

            _parserName = ReadInput("Parser name [goh] <enter to skip>: ");
            prefetchCount = ReadInputInt("Prefetch count [provide 0 use default setting]: ", 0);
            string base64EncodeDecode = ReadInput("Base64 encode/decode [enter to skip]: ", new string[] {"encode", "decode"}, true);
            if(!string.IsNullOrEmpty(base64EncodeDecode))
            {
                if (base64EncodeDecode.ToLower().Equals("encode"))
                    _actionBeforeWrite = ACTION.BASE64_ENCODE;
                else if (base64EncodeDecode.ToLower().Equals("decode"))
                    _actionBeforeWrite = ACTION.BASE64_DECODE;
                else
                    _actionBeforeWrite = ACTION.NOTHING;
            }

            ReadEmulatorInput();
            ReadStorageInput();
        }

        private static void ReadEmulatorInput()
        {
            ConsoleKeyInfo keyInfo = new ConsoleKeyInfo();
            do
            {
                Write("Select [Y] or [N] use emulator mode (do not complete message against the queue)", true);
                Write("Enter for default [Y]: ", false);
                keyInfo = Console.ReadKey(false);
                Write("", true);
            } while (keyInfo.Key != ConsoleKey.Y && keyInfo.Key != ConsoleKey.N && keyInfo.Key != ConsoleKey.Enter);

            if (keyInfo.Key == ConsoleKey.Y || keyInfo.Key == ConsoleKey.Enter)
                _emulatorMode = true;
            else
                _emulatorMode = false;

            if (_emulatorMode)
            {
                Write("Emulator mode selected!", true);
                Write("", true);
            }
            else
            {
                Write("Emulator disabled, messages will be REMOVED from queue!", true);
                Write("Proceed with caution", true);
                Write("", true);
            }
        }

        private static void ReadStorageInput()
        {
            ConsoleKeyInfo keyInfo = new ConsoleKeyInfo();

            do
            {
                Write("Press [1] to use File Storage or [2] to use blob storage (default)", true);
                Write("Enter for default [2]: ", false);
                keyInfo = Console.ReadKey(false);
                Write("", true);
            } while (keyInfo.Key != ConsoleKey.D1 && keyInfo.Key != ConsoleKey.D2 && keyInfo.Key != ConsoleKey.Enter);

            if (keyInfo.Key == ConsoleKey.D1)
                _useFileStorage = true;
            else
                _useFileStorage = false;

            if (_useFileStorage)
            {
                Write("File storage selected", true);
                _filePath = ReadInput("File path <enter space skip>: ");
                Write("", true);

                if (_filePath == null || string.IsNullOrEmpty(_filePath.Trim()))
                    throw new ArgumentException("File path is null or empty");
            }
            else
            {
                Write("Blob storage selected", true);
                _container = ReadInput("Container name <enter space skip>: ");
                Write("", true);

                if (_container == null || string.IsNullOrEmpty(_container.Trim()))
                    throw new ArgumentException("File path is null or empty");
            }
        }

        private static void CreateFileStorage(string filePath)
        {
            System.IO.Directory.CreateDirectory(filePath);
        }

        public static string ReadInput(string text)
        {
            string input = "";

            //do
            //{
                Console.Write(text);
                input = Console.ReadLine();
            //} while (string.IsNullOrEmpty(input) || input.Equals("\r\n")=;
            
            Console.WriteLine();

            return input;
        }

        public static bool ReaBooleanInput(string text, string[] validValues, bool defaultValue, bool enterToSkip)
        {
            string input = "";

            do
            {
                Console.Write(text);
                input = Console.ReadLine();
                if (string.IsNullOrEmpty(input) && enterToSkip)
                    return defaultValue;

            } while (string.IsNullOrEmpty(input) || !FindValue(input, validValues));

            Console.WriteLine();

            return bool.Parse(input);
        }

        public static string ReadInput(string text, string[] validValues, bool enterToSkip = true)
        {
            string input = "";

            do
            {
                Console.Write(text);
                input = Console.ReadLine();
                if (string.IsNullOrEmpty(input) && enterToSkip)
                    break;

            } while (string.IsNullOrEmpty(input) || !FindValue(input, validValues));

            Console.WriteLine();

            return input;
        }

        public static string ReadInput(string text, string[] validValues, string defaultValue, bool enterToSkip = true)
        {
            string input = "";

            do
            {
                Console.Write(text);
                input = Console.ReadLine();
                if (string.IsNullOrEmpty(input) && enterToSkip)
                    return defaultValue;

            } while (string.IsNullOrEmpty(input) || !FindValue(input, validValues));

            Console.WriteLine();

            return input;
        }

        private static bool FindValue(string value, string[] valueList)
        {
            for (int i = 0; i < valueList.Length; i++)
                if (value.ToLower().Equals(valueList[i].ToLower()))
                    return true;

            return false;
        }

        public static int ReadInputInt(string text)
        {
            string input = "";
            int result = 0;

            do
            {
                Console.Write(text);
                input = Console.ReadLine();
                if (!int.TryParse(input, out result))
                    Write("  Invalid input, provide a positive integer", true);
            } while (string.IsNullOrEmpty(input) || !int.TryParse(input, out result));

            Console.WriteLine();

            return result;
        }

        public static int ReadInputInt(string text, int minNumber)
        {
            string input = "";
            int result = 0;

            do
            {
                Console.Write(text);
                input = Console.ReadLine();
                if (!int.TryParse(input, out result) || int.Parse(input) < minNumber)
                    Write("  Invalid input, provide a positive integer", true);
            } while (string.IsNullOrEmpty(input) || !int.TryParse(input, out result) || int.Parse(input) < minNumber);

            Console.WriteLine();

            return result;
        }

        public static void Write(string text, bool CRLF)
        {
            string crlf = CRLF ? Environment.NewLine : "";
            Console.Write($"{text}{crlf}");
        }

        private static void ParseParameters(string[] parameters)
        {
            if(parameters == null || parameters.Length == 0)
            {
                _isInteractive = true;
                return;
            }    

            for(int i=0;i<parameters.Length;i++)
            {
                    switch(parameters[i])
                {
                    case "-purge":
                        _clearDlq = true; 
                        break;
                    case "-f":
                        _filePath = GetParamValue(parameters, i);
                        i++;
                        break;
                    case "-x":
                        _parserName = GetParamValue(parameters, i);
                        i++;
                        break;
                    case "-c":
                        _container = GetParamValue(parameters, i);
                        i++;
                        break;
                    case "-b":
                        string actionBeforeWrite = GetParamValue(parameters, i);
                        _actionBeforeWrite = readAction(actionBeforeWrite);
                        i++;
                        break;
                    case "-q":
                        _queue = GetParamValue(parameters, i);
                        i++;
                        break;
                    case "-e":
                        _useFileStorage = bool.Parse(GetParamValue(parameters, i));
                        i++;
                        break;
                    case "-interactive":
                        _isInteractive = true;
                        break;
                }
            }
        }

        private static ACTION readAction(string action)
        {
            if(action.ToLower().Equals("encode"))
                return ACTION.BASE64_DECODE;
            else if(action.ToLower().Equals("encode"))
                return ACTION.BASE64_ENCODE;

            return ACTION.NOTHING;
        }

        private static string GetParamValue(string[] parameters, int parameterIndex)
        {
            if (parameterIndex + 1 > (parameters.Length - 1))
                throw new ArgumentException($"Invalid value for '{parameters[parameterIndex]}'");

            return parameters[parameterIndex + 1];

        }

        private bool IsInteractive(string[] parameters)
        {
            foreach (string s in parameters)
                if (s.ToLower().Equals("-interactive"))
                    return true;

            return false;
        }
        public static void Welcome()
        {
            Write("==================================", true);
            Write("============DLQ READER============", true);
            Write("==================================", true);
            Write("", true);
            Usage();
            Write("During processing - press any key to quit!", true);
            Write("", true);
        }

        private static void Usage()
        {
            Write("Usage:", true);
            Write("  -x <parser>             (name of the parser to use)", true);
            Write("  -q <quename>            (name of the service bus queue)", true);
            Write("  -purge                  (purge the queue)", true);
            Write("  -c <blob container>     (name of blob container, overrides -f)", true);
            Write("  -f <absolute file path> (path to where files will be stored)", true);
            Write("  -e <true | false>       (emulator mode, default=true)", true);
            Write("  -p <number>             (prefetch count)", true);
            Write("  -b <encode | decode>    (base64 encode/decode before write)", true);
            Write("  -interactive            (will require you to input parameters)", true);
            Write("  ...no parameters at all will enforce interactive mode", true);
            Write("", true);
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            // Build configuration
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
               .AddJsonFile($"appsettings.json", false, true)
               //.AddJsonFile($"appsettings.{env}.json", true, true)
                .Build();

            // Add access to generic IConfigurationRoot
            //serviceCollection.AddSingleton<IConfigurationRoot>(configuration);

            // Add app
            serviceCollection.AddTransient<SBConsoleReader>();
        }

        

        private static void CreateBlobStorage(string containerName)
        {
            Write("Creating storage objects...", false);
            if (!CloudStorageAccount.TryParse(_storageConnectionString, out _storageAccount))
                throw new Exception("Could not create Cloud Storage Account - check connection string");

            CloudBlobClient _client = null;

            _client = _storageAccount.CreateCloudBlobClient();

            _storageContainer = _client.GetContainerReference(containerName);
            if (!_storageContainer.Exists())
                _storageContainer.Create();

            Write(" done!", true);
        }
        
        private static void CreateSB()
        {
            Write("Creating Service Bus Client...", true);
            _client = new ServiceBusClient(_serviceBusConnectionString);
        }

        private static async Task CreateClearQueueProcessor()
        {
            ServiceBusProcessorOptions pOptions = new ServiceBusProcessorOptions() { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete, PrefetchCount = 10, AutoCompleteMessages = true };

            if (_clearDlq) { 
                pOptions.SubQueue = SubQueue.DeadLetter;
            }

            processor = _client.CreateProcessor(_queue, pOptions);

            processor.ProcessMessageAsync += ClearMessageHandler;
            processor.ProcessErrorAsync += ErrorHandler;
        }

        // handle received messages
        static async Task ClearMessageHandler(ProcessMessageEventArgs args)
        {
            string Counter = GetIncreaseCounter().ToString().PadLeft(6, '0');
            Console.WriteLine($"Received [{Counter}]: {args.Message.MessageId}");
        }

        private static async Task CreateSBProcessor(string queue)
        {
            Write("Creating servicebus processor...", false);

            // create a processor that we can use to process the messages
            processor = _client.CreateProcessor(queue, new ServiceBusProcessorOptions() { SubQueue = SubQueue.DeadLetter, ReceiveMode = ServiceBusReceiveMode.PeekLock, AutoCompleteMessages = false, PrefetchCount = prefetchCount });
            Write(" Done!", true);

            processor.ProcessMessageAsync += MessageHandler;
            processor.ProcessErrorAsync += ErrorHandler;

            await processor.StartProcessingAsync();
            Write("Processing started!", true);
        }

    }
    
}
