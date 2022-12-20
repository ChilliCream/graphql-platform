using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline;

public sealed class ServiceConfiguration
{
    public ServiceConfiguration(string name, DocumentNode document)
        : this(name, new[] { document })
    {
    }

    public ServiceConfiguration(string name, IReadOnlyList<DocumentNode> documents)
    {
        Name = name;
        Documents = documents;
    }

    public string Name { get; }

    public IReadOnlyList<DocumentNode> Documents { get; }
}
