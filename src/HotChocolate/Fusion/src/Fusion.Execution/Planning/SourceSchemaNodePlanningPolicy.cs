using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning;

internal sealed record SourceSchemaNodePlanningPolicy(
    ImmutableHashSet<string>? AllowedSchemaNames,
    int? CandidateGroupId)
{
    public static SourceSchemaNodePlanningPolicy Descendant { get; } = new(null, null);

    public bool Allows(string schemaName)
        => AllowedSchemaNames is null || AllowedSchemaNames.Contains(schemaName);

    public SourceSchemaNodePlanningPolicy BindCandidate(string schemaName)
        => this with
        {
            AllowedSchemaNames = Allows(schemaName)
                ? ImmutableHashSet.Create(StringComparer.Ordinal, schemaName)
                : []
        };
}
