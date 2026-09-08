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
/// Resolves <c>argumentsCost</c> as the sum of a present argument's own
/// weight, paid once per call, and <c>directiveArgumentsCost</c> as the sum
/// of an applied directive's own definition arguments that are supplied or
/// carry a schema default; neither recurses into an input object's own
/// fields.
/// </remarks>
public sealed class CostAlgebra : IAnalysisAlgebra<CostEstimate>
{
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
        if (group.Field is not { } field || group.Member.ParentType is null || group.Member.Field is null)
        {
            return Empty;
        }

        var member = group.Member;
        var typeName = member.ParentType.Name;
        var fieldName = member.Field.Name;
        var returnTypeName = member.Field.Type.NamedType().Name;
        return CostFieldRule.Field(
            ResolveListMultiplier(typeName, fieldName, member.Field, field.Arguments, group.InheritedSize),
            _snapshot.GetFieldWeight(typeName, fieldName),
            ComputeArgumentsCost(typeName, member.Field, field.Arguments),
            ComputeDirectiveArgumentsCost(field.Directives),
            _snapshot.GetTypeWeight(returnTypeName),
            child);
    }

    /// <inheritdoc />
    public CostEstimate Combine(CostEstimate left, CostEstimate right) => CostFieldRule.Combine(left, right);

    /// <inheritdoc />
    public CostEstimate Join(CostEstimate left, CostEstimate right) => CostFieldRule.Join(left, right);

    /// <inheritdoc />
    public CostEstimate Root(double rootTypeWeight, CostEstimate selection) => CostFieldRule.Root(rootTypeWeight, selection);

    /// <summary>
    /// Resolves the list multiplier for one member's field call over its own
    /// <c>@listSize</c> metadata, the group's literal slicing-argument
    /// values, and the group's inherited <c>sizedFields</c> context.
    /// </summary>
    private double ResolveListMultiplier(
        string typeName,
        string fieldName,
        IOutputFieldDefinition field,
        IReadOnlyList<ArgumentNode> arguments,
        double? inheritedSize)
    {
        var isListField = field.Type.IsListType();
        var metadata = _snapshot.GetListSizeMetadata(typeName, fieldName);
        var slicingArguments = SlicingArgumentValues.Build(metadata, field, arguments);

        ReadOnlySpan<double> inheritedSizes = inheritedSize is { } size ? [size] : [];

        return ListSizeResolver.Resolve(
            isListField,
            metadata,
            inheritedSizes,
            slicingArguments,
            variableValues: null,
            _snapshot.Options.DefaultListSize);
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
            var effective = SlicingArgumentValues.FindArgumentValue(arguments, argumentDefinition.Name)
                ?? argumentDefinition.DefaultValue;

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
    /// of every directive applied to the field call whose value is supplied
    /// literally or that carries a schema default; a directive absent from
    /// the schema contributes nothing.
    /// </summary>
    private double ComputeDirectiveArgumentsCost(IReadOnlyList<DirectiveNode> directives)
    {
        var cost = 0.0;

        foreach (var directive in directives)
        {
            if (!_snapshot.TryGetDirectiveArguments(directive.Name.Value, out var definitionArguments))
            {
                continue;
            }

            foreach (var definitionArgument in definitionArguments)
            {
                var supplied = SlicingArgumentValues.FindArgumentValue(directive.Arguments, definitionArgument.Name) is not null;

                if (supplied || definitionArgument.HasDefaultValue)
                {
                    cost += definitionArgument.Weight;
                }
            }
        }

        return cost;
    }
}
