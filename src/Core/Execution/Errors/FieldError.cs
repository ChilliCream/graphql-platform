using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Newtonsoft.Json;

namespace HotChocolate.Execution
{
    public class FieldError
        : QueryError
    {
        public FieldError(
            string message,
            Path path,
            FieldNode fieldSelection,
            params KeyValuePair<string, string>[] extensions)
            : base(message)
        {
            Path = path.ToCollection();
            FieldName = fieldSelection.Name.Value;

            Locations = new[]
            {
                new Location(
                    fieldSelection.Location.StartToken.Line,
                    fieldSelection.Location.StartToken.Column)
            };

            var map = new Dictionary<string, string>
            {
                { "variableName", fieldSelection.Name.Value }
            };

            foreach (var extension in extensions)
            {
                map[extension.Key] = extension.Value;
            }

            Extensions = map;
        }

        [JsonProperty("path", Order = 2)]
        public IReadOnlyCollection<string> Path { get; }

        [JsonIgnore]
        public string FieldName { get; }

        [JsonProperty("locations", Order = 1)]
        public IReadOnlyCollection<Location> Locations { get; }

        [JsonProperty("extensions", Order = 3)]
        public IReadOnlyDictionary<string, string> Extensions { get; }
    }
}
