using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

public interface ISchemaMergeContext
{
    public IImmutableList<DocumentNode> Documents { get; set; }
}
