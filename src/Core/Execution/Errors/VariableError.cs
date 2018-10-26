using System.Collections.Generic;
using Newtonsoft.Json;

namespace HotChocolate.Execution
{
    public class VariableError
        : QueryError
    {
        public VariableError(string message, string variableName)
            : base(message)
        {
            VariableName = variableName;
            Extensions = new Dictionary<string, string>
            {
                { "variableName", variableName }
            };
        }

        [JsonIgnore]
        public string VariableName { get; }

        [JsonProperty("extensions")]
        public IReadOnlyDictionary<string, string> Extensions { get; }
    }
}
