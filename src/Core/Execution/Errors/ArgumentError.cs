using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Newtonsoft.Json;

namespace HotChocolate.Execution
{
    public class ArgumentError
        : FieldError
    {
        public ArgumentError(
            string message,
            Path path,
            FieldNode fieldSelection,
            string argumentName)
            : base(message, path, fieldSelection,
                new KeyValuePair<string, string>("argumentName", argumentName))
        {
            ArgumentName = argumentName;
        }

        [JsonIgnore]
        public string ArgumentName { get; }
    }
}
