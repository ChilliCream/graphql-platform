using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Wip;

public class ServiceDefinition : IServiceDefinition
{
    public ServiceDefinition(IServiceReference serviceReference, IEnumerable<DocumentNode> documents)
    {
        ServiceReference = serviceReference;
        Documents = documents;
    }

    public IServiceReference ServiceReference { get; }
    public IEnumerable<DocumentNode> Documents { get; }
}