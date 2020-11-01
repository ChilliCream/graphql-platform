using System.Collections.Generic;
using HotChocolate.Execution.Options;

namespace HotChocolate.Stitching.Redis
{
    internal sealed class SchemaDefinitionDto
    {
        public string Name { get; set; }

        public string? Document { get; set; }

        public List<string> ExtensionDocuments { get; } = new List<string>();
    }
}
