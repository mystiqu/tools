using console.input;
using console.input.domain;

internal class Program
{
    private static InputParameterParser _parser = null;
    private static bool _verbose = false;

    /// <summary>
    /// This app uses the InputParameterParser and InputSchema classes to show how they can be used in a real implementation
    /// </summary>
    /// <param name="args"></param>
    private static void Main(string[] args)
    {
        /*
            console.app is an application for loading generic files and calculating the size.

              -f: File path
              -r: Restrict to only these file extensions, comma separated string (sql,txt,cs,pdf etc)
              -v: Verbose logging
        */
        args = new string[] { "-f","testfile.txt", "-v", "-r", "txt" };

        _parser = DefineInputSchema(args);

        try
        {
            Console.WriteLine($"Complete param line: '{GetParams(args)}'");
            ReadFile(args);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        Console.WriteLine("----------------------------------------------" + Environment.NewLine);

        try
        {
            args = new string[] { "-f", "testfile.txt", "-r", "sql" };
            Console.WriteLine($"Complete param line: '{GetParams(args)}'");
            _parser = DefineInputSchema(args);
            ReadFile(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        Console.WriteLine("----------------------------------------------" + Environment.NewLine);

        try
        {
            args = new string[] { "-f", "testfile.txt", "-r", "sql,txt" };
            Console.WriteLine($"Complete param line: '{GetParams(args)}'");
            _parser = DefineInputSchema(args);
            ReadFile(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        Console.WriteLine("----------------------------------------------" + Environment.NewLine);

        try
        {
            args = new string[] {"-f" };
            Console.WriteLine($"Complete param line: '{GetParams(args)}'");
            _parser = DefineInputSchema(args);
            ReadFile(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }

    /// <summary>
    /// Read file size
    /// </summary>
    /// <param name="args">All arguments from the console input</param>
    /// <exception cref="ArgumentException"></exception>
    private static void ReadFile(string[] args)
    {
        _parser.Parse();
        if(args.Length == 0)
        {
            Console.WriteLine(_parser.GetHelpText());
            return;
        }

        if(!_parser.IsValid)
        {
            Console.WriteLine(_parser.ValidatonError);
            Console.WriteLine();

            Console.WriteLine(_parser.GetHelpText());

            return;
        }

        _verbose = _parser.Parameters.Where(x => x.Key.Equals("v")).Any();

        if (_verbose)
            Console.WriteLine("[VERBOSE] Verbose logging enabled");

        string fileName = _parser.GetPropertyValue("f");

        string restrictions = "";
        if((restrictions = _parser.GetPropertyValue("r")).Length > 0)
        {
            if (_verbose)
                Console.WriteLine($"[VERBOSE] File extension restriction set to: {_parser.GetPropertyValue("-r")}");

            if (!validateFileExtension(fileName, restrictions))
                throw new ArgumentException($"Input filename not restricted to the following file extensions: {restrictions}");

        }
        

        if (_verbose)
            Console.WriteLine($"[VERBOSE] Filename supplied by input: '{fileName}'");

        Console.WriteLine($"[INFO] Parsing file '{fileName}'");

        long size = GetFileSize(fileName);

        Console.WriteLine($"[INFO] '{fileName}' has the total size of {size.ToString()} bytes");
    }

    /// <summary>
    /// Concats all the input params to be able to print them nicely
    /// </summary>
    /// <param name="args">Console input arguments</param>
    /// <returns>The concatenated string</returns>
    private static string GetParams(string[] args)
    {
        string completeParamLine = "";
        for (int i = 0; i < args.Length; i++)
            completeParamLine += args[i] + " ";

        return completeParamLine;
    }
    private static InputParameterParser DefineInputSchema(string[] args)
    {
        InputSchema inputSchema = new InputSchema();
        inputSchema.PropertyPrefix = '-';
        inputSchema.Description = "console.app is an application for loading generic files and calculating the size.";
        inputSchema.Properties.Add(new InputProperty() { Key = "f", Type = PROPERTY_TYPE.KEY_VALUE, Required = true, HelpText = "Path to file" });
        inputSchema.Properties.Add(new InputProperty() { Key = "r", Type = PROPERTY_TYPE.KEY_VALUE, Required = false, HelpText = "Restrict to only these file extensions, comma separated string (sql,txt,cs,pdf etc)" });
        inputSchema.Properties.Add(new InputProperty() { Key = "v", Type = PROPERTY_TYPE.KEY_ONLY, Required = false, HelpText = "Verbose - extra logging" });

        _parser = new InputParameterParser(inputSchema, args);

        return _parser;
    }

    /// <summary>
    /// Validates the file extension against the supplied list of allows extensions
    /// </summary>
    /// <param name="fileName">The filename</param>
    /// <param name="fileExtensions">The allowed file extensions</param>
    /// <returns></returns>
    private static bool validateFileExtension(string fileName, string fileExtensions)
    {
        string fileExt = fileName.Substring(fileName.LastIndexOf(".") + 1);

        if (string.IsNullOrEmpty(fileExtensions))
            return true;

        return fileExtensions.Contains(fileExt);
    }

    /// <summary>
    /// Get the file size in bytes from a file
    /// </summary>
    /// <param name="fileName">The filename</param>
    /// <returns>The size in bytes</returns>
    private static long GetFileSize(string fileName)
    {
        FileInfo fileInfo = new FileInfo(fileName);

        return fileInfo.Length;
    }
}