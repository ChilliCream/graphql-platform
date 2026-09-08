using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// The built-in IBM field/type cost analysis algebra: the lean field rule
/// (<see cref="CostFieldRule"/>) applied over weights and list multipliers
/// resolved from a <see cref="CostSchemaSnapshot"/>
/// (<see cref="ListSizeResolver"/>).
/// </summary>
/// <remarks>
/// Resolves <c>argumentsCost</c> and <c>directiveArgumentsCost</c> as the
/// sum of an argument's or directive argument's own weight, paid once per
/// call for every argument present after coercion; it does not recurse into
/// an input object's own fields.
/// </remarks>
public sealed class CostAlgebra : IAnalysisAlgebra<CostEstimate>
{
    private static readonly Dictionary<string, SlicingArgumentValue> NoSlicingArguments = [];

    private readonly CostSchemaSnapshot _snapshot;

    /// <summary>
    /// Initializes a new instance of <see cref="CostAlgebra"/>.
    /// </summary>
    /// <param name="snapshot">
    /// The schema snapshot to resolve weights and list-size metadata
    /// against.
    /// </param>
    public CostAlgebra(CostSchemaSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _snapshot = snapshot;
    }

    /// <inheritdoc />
    public CostEstimate Empty => CostFieldRule.Empty;

    /// <inheritdoc />
    public CostEstimate Field(in CollectedFieldGroup group, CostEstimate child)
    {
        var fieldWeight = double.NegativeInfinity;
        var returnTypeWeight = double.NegativeInfinity;
        var argumentsCost = double.NegativeInfinity;
        var n = double.NegativeInfinity;

        foreach (var member in group.Members)
        {
            var typeName = member.ParentType.Name;
            var fieldName = member.Field.Name;
            var returnTypeName = member.Field.Type.NamedType().Name;

            fieldWeight = Math.Max(fieldWeight, _snapshot.GetFieldWeight(typeName, fieldName));
            returnTypeWeight = Math.Max(returnTypeWeight, _snapshot.GetTypeWeight(returnTypeName));
            argumentsCost = Math.Max(argumentsCost, ComputeArgumentsCost(typeName, member.Field, group.Arguments));
            n = Math.Max(n, ResolveListMultiplier(typeName, fieldName, member.Field, group.Arguments));
        }

        var directiveArgumentsCost = ComputeDirectiveArgumentsCost(group.Directives);

        return CostFieldRule.Field(n, fieldWeight, argumentsCost, directiveArgumentsCost, returnTypeWeight, child);
    }

    /// <inheritdoc />
    public CostEstimate Combine(CostEstimate left, CostEstimate right) => CostFieldRule.Combine(left, right);

    /// <inheritdoc />
    public CostEstimate Join(CostEstimate left, CostEstimate right) => CostFieldRule.Join(left, right);

    /// <inheritdoc />
    public CostEstimate Root(double rootTypeWeight, CostEstimate selection) => CostFieldRule.Root(rootTypeWeight, selection);

    /// <summary>
    /// Resolves the list multiplier for one member's field call over its own
    /// <c>@listSize</c> metadata and the group's literal slicing-argument
    /// values, with no inherited <c>sizedFields</c> context.
    /// </summary>
    private double ResolveListMultiplier(
        string typeName,
        string fieldName,
        IOutputFieldDefinition field,
        IReadOnlyList<ArgumentNode> arguments)
    {
        var isListField = field.Type.IsListType();
        var metadata = _snapshot.GetListSizeMetadata(typeName, fieldName);
        var slicingArguments = metadata is { SlicingArguments.Length: > 0 }
            ? BuildSlicingArguments(metadata, field, arguments)
            : NoSlicingArguments;

        return ListSizeResolver.Resolve(
            isListField,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues: null,
            _snapshot.Options.DefaultListSize);
    }

    /// <summary>
    /// Builds the slicing-argument lookup <see cref="ListSizeResolver.Resolve"/>
    /// needs from <paramref name="metadata"/>'s declared slicing arguments,
    /// pairing each one's literal supplied value with its schema default.
    /// </summary>
    private static Dictionary<string, SlicingArgumentValue> BuildSlicingArguments(
        ListSizeMetadata metadata,
        IOutputFieldDefinition field,
        IReadOnlyList<ArgumentNode> arguments)
    {
        var result = new Dictionary<string, SlicingArgumentValue>(metadata.SlicingArguments.Length);

        foreach (var name in metadata.SlicingArguments)
        {
            var schemaDefault = field.Arguments.TryGetField(name, out var argumentDefinition)
                ? argumentDefinition.DefaultValue
                : null;
            result[name] = new SlicingArgumentValue(FindArgumentValue(arguments, name), schemaDefault);
        }

        return result;
    }

    /// <summary>
    /// Sums a field's own weight for every argument present after coercion:
    /// a literal supplied value, or the argument's schema default when it
    /// was omitted. An explicit literal <c>null</c> is present and charged;
    /// an argument with neither a supplied value nor a schema default is
    /// absent and contributes nothing.
    /// </summary>
    private double ComputeArgumentsCost(string typeName, IOutputFieldDefinition field, IReadOnlyList<ArgumentNode> arguments)
    {
        var cost = 0.0;

        foreach (var argumentDefinition in field.Arguments)
        {
            var effective = FindArgumentValue(arguments, argumentDefinition.Name) ?? argumentDefinition.DefaultValue;

            if (effective is null)
            {
                continue;
            }

            cost += _snapshot.GetArgumentWeight(typeName, field.Name, argumentDefinition.Name);
        }

        return cost;
    }

    /// <summary>
    /// Sums a directive-definition argument's own weight for every argument
    /// literally present on every directive applied to the field call.
    /// </summary>
    private double ComputeDirectiveArgumentsCost(IReadOnlyList<DirectiveNode> directives)
    {
        var cost = 0.0;

        foreach (var directive in directives)
        {
            foreach (var argument in directive.Arguments)
            {
                cost += _snapshot.GetDirectiveArgumentWeight(directive.Name.Value, argument.Name.Value);
            }
        }

        return cost;
    }

    private static IValueNode? FindArgumentValue(IReadOnlyList<ArgumentNode> arguments, string name)
    {
        foreach (var argument in arguments)
        {
            if (string.Equals(argument.Name.Value, name, StringComparison.Ordinal))
            {
                return argument.Value;
            }
        }

        return null;
    }
}
