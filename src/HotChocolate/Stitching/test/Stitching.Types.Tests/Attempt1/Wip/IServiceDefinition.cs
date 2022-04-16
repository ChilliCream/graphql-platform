using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Wip;

public interface IServiceDefinition
{
    IServiceReference ServiceReference { get; }
    IEnumerable<DocumentNode> Documents { get; }
}