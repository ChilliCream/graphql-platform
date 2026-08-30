using HotChocolate.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Describes the guarded resource of a policy evaluation: the type and, when the application
/// guards a field, the selection that produced it, together with the batch of entities to
/// evaluate and the variables of the request.
/// </summary>
/// <remarks>
/// An instance is only valid for the duration of the <see cref="IPolicy.EvaluateAsync"/> call it
/// was reached through and must not be used after that call has completed.
/// </remarks>
public sealed class PolicySelection
{
    private ITypeDefinition _type = null!;
    private ISelection? _selection;
    private IVariableValueCollection _variables = null!;
    private ReadOnlyMemory<CompositeResultElement> _entities;

    internal PolicySelection()
    {
    }

    /// <summary>
    /// Gets the type definition the policy application targets.
    /// </summary>
    public ITypeDefinition Type => _type;

    /// <summary>
    /// Gets the selection the policy application guards, or <c>null</c> when the policy
    /// guards a type definition rather than a selection.
    /// </summary>
    public ISelection? Selection => _selection;

    /// <summary>
    /// Gets the variables of the request that triggered the evaluation.
    /// </summary>
    public IVariableValueCollection Variables => _variables;

    /// <summary>
    /// Gets the entities to evaluate. Never empty.
    /// </summary>
    public ReadOnlyMemory<CompositeResultElement> Entities => _entities;

    internal void Reset(
        ITypeDefinition type,
        ISelection? selection,
        IVariableValueCollection variables,
        ReadOnlyMemory<CompositeResultElement> entities)
    {
        _type = type;
        _selection = selection;
        _variables = variables;
        _entities = entities;
    }

    internal void Clear()
    {
        _type = null!;
        _selection = null;
        _variables = null!;
        _entities = default;
    }
}
