using HotChocolate.Execution;
using static HotChocolate.Fusion.Properties.FusionExecutionResources;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Thrown when a <c>@defer</c>d fragment is anchored on a mutation result whose
/// anchor type has no lookup in the composite schema.
/// </summary>
public sealed class DeferredMutationLookupRequiredException : Exception
{
    public DeferredMutationLookupRequiredException(SelectionPath path, string typeName)
        : base(FormatMessage(path, typeName))
    {
        Path = path;
        TypeName = typeName;
    }

    private static string FormatMessage(SelectionPath path, string typeName)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        return string.Format(
            DeferredMutationLookupRequiredException_NoLookupAvailable,
            path,
            typeName);
    }

    /// <summary>
    /// Gets the selection path of the unresolvable defer anchor.
    /// </summary>
    public SelectionPath Path { get; }

    /// <summary>
    /// Gets the name of the anchor type that has no lookup.
    /// </summary>
    public string TypeName { get; }
}
