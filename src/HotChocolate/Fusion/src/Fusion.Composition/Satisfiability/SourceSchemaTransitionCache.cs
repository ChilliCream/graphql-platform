using System.Collections.Immutable;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Memoizes the direct lookup portion of a source schema transition for a single validation run.
/// A cached entry records whether a direct lookup on a type is possible in a target source schema,
/// given the source schema the traversal arrived from, together with the errors describing the
/// lookups that were tried when it is not.
/// </summary>
internal sealed class SourceSchemaTransitionCache
{
    private readonly Dictionary<TransitionKey, DirectLookupResult> _directLookups = [];

    /// <summary>
    /// Attempts to get the cached direct lookup result for a transition.
    /// </summary>
    /// <param name="type">The type the transition needs to reach in the target schema.</param>
    /// <param name="targetSchemaName">The source schema the transition moves to.</param>
    /// <param name="previousSchemaName">The source schema the traversal arrived from.</param>
    /// <param name="result">The cached result when one exists.</param>
    /// <returns><c>true</c> if a cached result exists; otherwise, <c>false</c>.</returns>
    public bool TryGetDirectLookup(
        MutableObjectTypeDefinition type,
        string targetSchemaName,
        string? previousSchemaName,
        out DirectLookupResult result)
    {
        return _directLookups.TryGetValue(
            new TransitionKey(type, targetSchemaName, previousSchemaName),
            out result);
    }

    /// <summary>
    /// Stores the direct lookup result for a transition.
    /// </summary>
    /// <param name="type">The type the transition needs to reach in the target schema.</param>
    /// <param name="targetSchemaName">The source schema the transition moves to.</param>
    /// <param name="previousSchemaName">The source schema the traversal arrived from.</param>
    /// <param name="result">The result to store.</param>
    public void AddDirectLookup(
        MutableObjectTypeDefinition type,
        string targetSchemaName,
        string? previousSchemaName,
        DirectLookupResult result)
    {
        _directLookups[new TransitionKey(type, targetSchemaName, previousSchemaName)] = result;
    }

    private readonly record struct TransitionKey(
        MutableObjectTypeDefinition Type,
        string TargetSchemaName,
        string? PreviousSchemaName);
}

/// <summary>
/// The memoized outcome of the direct lookup portion of a source schema transition.
/// </summary>
/// <param name="Satisfiable">Whether a direct lookup on the type is possible.</param>
/// <param name="Errors">
/// When not satisfiable, the errors describing every direct lookup that was tried; otherwise empty.
/// </param>
internal readonly record struct DirectLookupResult(
    bool Satisfiable,
    ImmutableArray<SatisfiabilityError> Errors);
