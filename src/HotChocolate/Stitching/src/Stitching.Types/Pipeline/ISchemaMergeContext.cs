using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline;

public interface ISchemaMergeContext
{
    IReadOnlyList<ServiceConfiguration> Configurations { get; }

    IImmutableList<DocumentNode> Documents { get; set; }

    ICollection<IError> Errors { get; }
}
