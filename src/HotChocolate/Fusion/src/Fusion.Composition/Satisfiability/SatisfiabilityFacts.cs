using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

internal sealed class SatisfiabilityFacts
{
    private readonly HashSet<CanTransitionFactKey> _canTransitionFacts;
    private readonly HashSet<FieldAccessibleFactKey> _fieldAccessibleFacts;
    private readonly HashSet<FieldResolvableFactKey> _fieldResolvableFacts;

    internal SatisfiabilityFacts(
        HashSet<CanTransitionFactKey> canTransitionFacts,
        HashSet<FieldAccessibleFactKey> fieldAccessibleFacts,
        HashSet<FieldResolvableFactKey> fieldResolvableFacts)
    {
        _canTransitionFacts = [.. canTransitionFacts];
        _fieldAccessibleFacts = [.. fieldAccessibleFacts];
        _fieldResolvableFacts = [.. fieldResolvableFacts];
    }

    /// <summary>
    /// Determines whether an entity of <paramref name="type"/> held on
    /// <paramref name="fromSchema"/> can transition to <paramref name="targetSchema"/>.
    /// </summary>
    /// <param name="type">The object type being transitioned.</param>
    /// <param name="targetSchema">The source schema to transition to.</param>
    /// <param name="fromSchema">The source schema the entity is currently held on.</param>
    /// <returns><see langword="true"/> when the transition is satisfiable.</returns>
    public bool CanTransition(
        MutableObjectTypeDefinition type,
        string targetSchema,
        string fromSchema)
        => _canTransitionFacts.Contains(new CanTransitionFactKey(type, targetSchema, fromSchema));

    /// <summary>
    /// Determines whether <paramref name="field"/> can be resolved on
    /// <paramref name="type"/> when the entity is held on <paramref name="fromSchema"/>.
    /// </summary>
    /// <param name="type">The object type declaring the field.</param>
    /// <param name="field">The field whose accessibility is queried.</param>
    /// <param name="fromSchema">The source schema the entity is currently held on.</param>
    /// <returns><see langword="true"/> when the field is accessible.</returns>
    public bool IsFieldAccessible(
        MutableObjectTypeDefinition type,
        MutableOutputFieldDefinition field,
        string fromSchema)
        => _fieldAccessibleFacts.Contains(new FieldAccessibleFactKey(type, field, fromSchema));

    /// <summary>
    /// Determines whether <paramref name="field"/> is resolvable as the entity's own field on
    /// <paramref name="sourceSchema"/>, that is the field exists there, is not partial, and its
    /// <c>@require</c> is satisfied. This does not account for reaching
    /// <paramref name="sourceSchema"/>; see <see cref="CanTransition"/> for that.
    /// </summary>
    /// <param name="type">The object type declaring the field.</param>
    /// <param name="field">The field whose resolvability is queried.</param>
    /// <param name="sourceSchema">The source schema the field is resolved on.</param>
    /// <returns><see langword="true"/> when the field is resolvable on the schema.</returns>
    public bool IsFieldResolvableOn(
        MutableObjectTypeDefinition type,
        MutableOutputFieldDefinition field,
        string sourceSchema)
        => _fieldResolvableFacts.Contains(new FieldResolvableFactKey(type, field, sourceSchema));

    internal readonly record struct CanTransitionFactKey(
        MutableObjectTypeDefinition Type,
        string TargetSchema,
        string FromSchema);

    internal readonly record struct FieldAccessibleFactKey(
        MutableObjectTypeDefinition Type,
        MutableOutputFieldDefinition Field,
        string FromSchema);

    internal readonly record struct FieldResolvableFactKey(
        MutableObjectTypeDefinition Type,
        MutableOutputFieldDefinition Field,
        string SourceSchema);
}
