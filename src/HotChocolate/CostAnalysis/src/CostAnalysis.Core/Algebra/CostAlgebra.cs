using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Computes field and type costs using the IBM GraphQL cost specification.
/// </summary>
/// <remarks>
/// Supplied arguments and arguments with schema defaults contribute their weights once
/// per field call, including arguments of applied directives. Input object fields
/// contribute costs based on their coerced values.
/// </remarks>
public sealed class CostAlgebra : IAnalysisAlgebra<CostEstimate>
{
    private readonly CostSchemaIndex _schemaIndex;
    private readonly ICostVariableValues? _variableValues;

    /// <summary>
    /// Creates a cost analysis that uses schema assumptions for variable values.
    /// </summary>
    /// <param name="schemaIndex">
    /// The schema index to resolve weights and list-size metadata
    /// against.
    /// </param>
    public CostAlgebra(CostSchemaIndex schemaIndex)
    {
        ArgumentNullException.ThrowIfNull(schemaIndex);
        _schemaIndex = schemaIndex;
        _variableValues = null;
    }

    /// <summary>
    /// Creates a cost analysis that uses the request's coerced variable values.
    /// </summary>
    /// <param name="schemaIndex">
    /// The schema index to resolve weights and list-size metadata
    /// against.
    /// </param>
    /// <param name="variableValues">
    /// The coerced variable values of the request.
    /// </param>
    [Experimental(CostExperiments.AnalysisAlgebra)]
    public CostAlgebra(CostSchemaIndex schemaIndex, ICostVariableValues variableValues)
    {
        ArgumentNullException.ThrowIfNull(schemaIndex);
        ArgumentNullException.ThrowIfNull(variableValues);
        _schemaIndex = schemaIndex;
        _variableValues = variableValues;
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
        var estimate = CostFieldRule.Field(
            ResolveListMultiplier(typeName, fieldName, member.Field, field.Arguments, group.InheritedSize),
            _schemaIndex.GetFieldWeight(typeName, fieldName),
            ComputeArgumentsCost(typeName, member.Field, field.Arguments),
            ComputeDirectiveArgumentsCost(field.Directives),
            _schemaIndex.GetTypeWeight(returnTypeName),
            child);

        return CostFieldRule.Join(CostFieldRule.Empty, estimate);
    }

    /// <inheritdoc />
    public CostEstimate Combine(CostEstimate left, CostEstimate right)
        => CostFieldRule.Combine(left, right);

    /// <inheritdoc />
    public CostEstimate Join(CostEstimate left, CostEstimate right)
        => CostFieldRule.Join(left, right);

    /// <inheritdoc />
    public CostEstimate Root(double rootTypeWeight, CostEstimate selection)
        => CostFieldRule.Root(rootTypeWeight, selection);

    /// <summary>
    /// Gets a field's list multiplier from its list-size annotation, slicing arguments,
    /// and inherited size.
    /// </summary>
    private double ResolveListMultiplier(
        string typeName,
        string fieldName,
        IOutputFieldDefinition field,
        IReadOnlyList<ArgumentNode> arguments,
        double? inheritedSize)
    {
        var isListField = field.Type.IsListType();
        var metadata = _schemaIndex.GetListSizeMetadata(typeName, fieldName);
        var slicingArguments = SlicingArgumentValues.Build(metadata, field, arguments);

        ReadOnlySpan<double> inheritedSizes = inheritedSize is { } size ? [size] : [];

        return ListSizeResolver.Resolve(
            isListField,
            metadata,
            inheritedSizes,
            slicingArguments,
            _variableValues,
            _schemaIndex.DefaultListSize);
    }

    /// <summary>
    /// Computes the cost of supplied field arguments and arguments with schema defaults.
    /// An explicit null is supplied and contributes cost; an omitted argument without a default does not.
    /// </summary>
    private double ComputeArgumentsCost(
        string typeName,
        IOutputFieldDefinition field,
        IReadOnlyList<ArgumentNode> arguments)
    {
        var cost = 0.0;

        foreach (var argument in _schemaIndex.GetFieldArguments(typeName, field.Name))
        {
            cost += InputCost.Compute(
                _schemaIndex,
                argument,
                SlicingArgumentValues.FindArgumentValue(arguments, argument.Name),
                _variableValues);
        }

        return cost;
    }

    /// <summary>
    /// Computes the cost of supplied arguments and schema defaults for directives applied to a field.
    /// Directives absent from the schema contribute no cost.
    /// </summary>
    private double ComputeDirectiveArgumentsCost(IReadOnlyList<DirectiveNode> directives)
    {
        var cost = 0.0;

        foreach (var directive in directives)
        {
            if (!_schemaIndex.TryGetDirectiveArgumentMetadata(directive.Name.Value, out var definitionArguments))
            {
                continue;
            }

            foreach (var argument in definitionArguments)
            {
                cost += InputCost.Compute(
                    _schemaIndex,
                    argument,
                    SlicingArgumentValues.FindArgumentValue(directive.Arguments, argument.Name),
                    _variableValues);
            }
        }

        return cost;
    }
}
