using System.Collections.Immutable;

namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// The authorization requirements of the selections of an operation and the synthetic variables
/// that skip denied selections in source schema requests.
/// </summary>
public sealed class OperationAuthorization
{
    internal OperationAuthorization(
        ImmutableArray<PolicyDescriptor> descriptors,
        ImmutableArray<AuthorizationVariable> variables)
    {
        Descriptors = descriptors;
        Variables = variables;
    }

    /// <summary>
    /// Gets one descriptor for every directive occurrence of every protected selection.
    /// </summary>
    public ImmutableArray<PolicyDescriptor> Descriptors { get; }

    /// <summary>
    /// Gets the synthetic variables in the order they were allocated.
    /// </summary>
    public ImmutableArray<AuthorizationVariable> Variables { get; }
}
