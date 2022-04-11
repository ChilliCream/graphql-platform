using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

public interface IServiceDefinition
{
    IServiceReference ServiceReference { get; }
    IEnumerable<DocumentNode> Documents { get; }
}