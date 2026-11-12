using System.Collections.Immutable;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Describes where the value of a context name used by an <see cref="OperationRequirement"/>
/// comes from.
/// </summary>
/// <param name="Name">
/// The name of the context.
/// </param>
/// <param name="Path">
/// The selection path of the value that sets the context.
/// </param>
/// <param name="TypeNames">
/// The runtime type names the root is restricted to. Empty when every value at
/// <paramref name="Path"/> sets the context.
/// </param>
public record RequirementContextRoot(
    string Name,
    SelectionPath Path,
    ImmutableArray<string> TypeNames);
