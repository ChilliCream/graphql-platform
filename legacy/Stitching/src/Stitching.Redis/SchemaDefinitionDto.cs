using System.Collections.Generic;

namespace HotChocolate.Stitching.Redis
{
    internal sealed class SchemaDefinitionDto
    {
        public string? Name { get; set; }

        public string? Document { get; set; }

        public List<string> ExtensionDocuments { get; set; } = new List<string>();
    }
}
