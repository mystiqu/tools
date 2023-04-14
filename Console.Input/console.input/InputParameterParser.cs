using console.input.domain;

namespace console.input
{
    public class InputParameterParser
    {
        public List<KeyValuePair<string, string>> Parameters { get; set; }
        private string[] _inputParameters;
        private InputSchema _inputSchema;
        public string ValidatonError { get; set; }
        public bool IsValid { get; set; }
        public InputParameterParser(params string[] inputParemeters)
        {
            Parameters = new List<KeyValuePair<string, string>>();
            _inputParameters = inputParemeters;
            ValidatonError = "";
            IsValid = true;
            _inputSchema = new InputSchema();
        }

        public InputParameterParser(InputSchema parameterSchema, params string[] inputParemeters) : this(inputParemeters)
        {
            _inputSchema = parameterSchema;
        }

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

        public KeyValuePair<string,string> GetProperty(string key)
        {
            return Parameters.Where(x => x.Key.Equals(key)).FirstOrDefault();
        }

        public string GetPropertyValue(string key)
        {
            IEnumerable<KeyValuePair<string, string>> res = Parameters.Where(x => x.Key.Equals(key));
            if(res.Any())  
                return res.First().Value;

            return String.Empty;
        }

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

        public string GetValue(string key)
        {
            return String.Empty;
        }

    }
}