using console.input.domain;

namespace console.input
{
    /// <summary>
    /// Class that parses input parameters and (optionally) validates them against a supplied schema
    /// </summary>
    public class InputParameterParser
    {
        public List<KeyValuePair<string, string>> Parameters { get; set; }
        private string[] _inputParameters;
        private InputSchema _inputSchema;
        public string ValidatonError { get; set; }
        public bool IsValid { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="inputParemeters">The input parameters, usually the args string list from a console application</param>
        public InputParameterParser(params string[] inputParemeters)
        {
            Parameters = new List<KeyValuePair<string, string>>();
            _inputParameters = inputParemeters;
            ValidatonError = "";
            IsValid = true;
            _inputSchema = new InputSchema();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parameterSchema">The parameter schema that defines which parameters are available, their usage and type</param>
        /// <param name="inputParemeters">The input parameters, usually the args string list from a console application</param>

        public InputParameterParser(InputSchema parameterSchema, params string[] inputParemeters) : this(inputParemeters)
        {
            _inputSchema = parameterSchema;
        }

        /// <summary>
        /// Parses the parameters and (optionally) validates against the schema
        /// </summary>
        public void Parse()
        {
            KeyValuePair<string, string> current;
            for (int i = 0; i < _inputParameters.Length; i++)
            {
                //Check if it's a value or steering parameter
                if (_inputParameters[i][0] == _inputSchema.PropertyPrefix) //steering oarameter
                {
                    if (i < _inputParameters.Length-1 && _inputParameters[i + 1][0] != _inputSchema.PropertyPrefix)
                        current = new KeyValuePair<string, string>(_inputParameters[i].TrimStart(_inputSchema.PropertyPrefix), _inputParameters[i + 1]);
                    else
                        current = new KeyValuePair<string, string>(_inputParameters[i].TrimStart(_inputSchema.PropertyPrefix), "");

                    Parameters.Add(current);
                }
            }

            if(!_inputSchema.DefaultSchema) //If no proper schema is present, skip the validation phase
                IsValid = Validate();
        }

        /// <summary>
        /// Validates the properties against the schema. Use only of schema is used
        /// </summary>
        /// <returns>true | false depending on the result</returns>
        private bool Validate()
        {
            foreach(KeyValuePair<string,string> inputVal in Parameters)
            {
                InputProperty prop = GetSchemaProperty(inputVal.Key);
                if (prop.Type == PROPERTY_TYPE.KEY_VALUE && string.IsNullOrEmpty(inputVal.Value))
                {
                    ValidatonError = $"Key '{inputVal.Key}' is expected to have a value";
                    return false;
                }
                else if (prop.Type == PROPERTY_TYPE.KEY_ONLY && !string.IsNullOrEmpty(inputVal.Value))
                {
                    ValidatonError = $"Key '{inputVal.Key}' is not expected to have a value";
                    return false;
                }
                else if (prop.Required && string.IsNullOrEmpty(inputVal.Value))
                {
                    ValidatonError = $"Key '{inputVal.Key}' is required to have a value";
                    return false;
                }
            }

           foreach(InputProperty prop in _inputSchema.Properties)
                if(prop.Required)
                {
                    IEnumerable<KeyValuePair<string,string>> res = Parameters.Where(x => x.Key == prop.Key);
                    
                    if (!res.Any())
                    {
                        ValidatonError = $"Key '{prop.Key}' is required but not present";
                        return false;
                    }
                    else if(string.IsNullOrEmpty(res.First().Value))
                    {
                        ValidatonError = $"Key '{prop.Key}' is required to have a value";
                        return false;
                    }
                }
            
            return true;
        }

        /// <summary>
        /// Get a specific property (e.g. -f) based on the key name. Supports with/without parameter prefix
        /// </summary>
        /// <param name="key">The key, e,g, -f or just f</param>
        /// <returns>KeyValuePair representing the property</returns>
        public KeyValuePair<string,string> GetProperty(string key)
        {
            string internalKey = key;
            if (internalKey.StartsWith(_inputSchema.PropertyPrefix))
                internalKey = key.TrimStart(_inputSchema.PropertyPrefix);

            return Parameters.Where(x => x.Key.Equals(internalKey)).FirstOrDefault();
        }

        /// <summary>
        /// Get a property value based on the key, e.g. -f or just f
        /// </summary>
        /// <param name="key">The key, e,g, -f or just f</param>
        /// <returns>The value</returns>
        public string GetPropertyValue(string key)
        {
            string internalKey = key;
            if (internalKey.StartsWith(_inputSchema.PropertyPrefix))
                internalKey = key.TrimStart(_inputSchema.PropertyPrefix);

            IEnumerable<KeyValuePair<string, string>> res = Parameters.Where(x => x.Key.Equals(internalKey));
            if(res.Any())  
                return res.First().Value;

            return String.Empty;
        }

        /// <summary>
        /// The a specific property/definition from the schema
        /// </summary>
        /// <param name="key">The key to fetch the definition for</param>
        /// <returns>InputProperty instance</returns>
        /// <exception cref="Exception"></exception>
        private InputProperty GetSchemaProperty(string key)
        {
            IEnumerable<InputProperty> res = _inputSchema.Properties.Where(x => x.Key.Equals(key));
            if (res.Count() > 1)
                throw new Exception($"Multiple input parameters found: '{key}'");
            if (res.Any())
                return res.First();

            else
                throw new Exception($"No schema definition found for input parameter: '{key}'");
        }

        /// <summary>
        /// Returns the help text for all parameters defined in the schema.
        /// </summary>
        /// <returns></returns>
        public string GetHelpText()
        {
            if (_inputSchema.DefaultSchema)
                return "No schema defined for input parameters";

            string helpText = string.Empty;

            helpText = Environment.NewLine + _inputSchema.Description;
            helpText += Environment.NewLine + Environment.NewLine;
            string commandText = string.Empty;

            foreach(InputProperty prop in _inputSchema.Properties)
            {
                commandText = "  " + _inputSchema.PropertyPrefix + prop.Key;
                commandText += ": " + prop.HelpText;
                helpText += commandText + Environment.NewLine;
            }

            return helpText;
        }

        /// <summary>
        /// Get the help text for a specific parameter
        /// </summary>
        /// <param name="key">Key name, e.g. -f or just f</param>
        /// <returns>The help text</returns>
        public string GetHelpText(string key)
        {
            if (_inputSchema.DefaultSchema)
                return "No schema defined for input parameters";

            string internalKey = key;
            string commandText = string.Empty;

            if (key.StartsWith(_inputSchema.PropertyPrefix))
                internalKey = key.TrimStart(_inputSchema.PropertyPrefix);

            IEnumerable<InputProperty> props = _inputSchema.Properties.Where(x => x.Key.Equals(internalKey));
            if (props.Any())
            {
                commandText = "  " + _inputSchema.PropertyPrefix + props.First().Key;
                commandText += ": " + props.First().HelpText;

                return commandText;
            }

            return $"Could not find a help text for key '{_inputSchema.PropertyPrefix}{key}'.";  
        }

    }
}