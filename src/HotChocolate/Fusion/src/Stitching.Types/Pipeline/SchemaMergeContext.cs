using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline;

internal sealed class SchemaMergeContext : ISchemaMergeContext
{
    public SchemaMergeContext(IReadOnlyList<ServiceConfiguration> configurations)
    {
        Configurations = configurations;
    }

    public IReadOnlyList<ServiceConfiguration> Configurations { get; }

    public IImmutableList<Document> Documents { get; set; } = ImmutableList<Document>.Empty;

    public ICollection<IError> Errors { get; } = new List<IError>();

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();
}
