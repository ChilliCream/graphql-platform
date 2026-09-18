using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// The built-in maximum response-size analysis algebra.
/// </summary>
public sealed class ResponseSizeAlgebra : IAnalysisAlgebra<double>
{
    private readonly CostSchemaIndex _schemaIndex;
    private readonly ICostVariableValues? _variableValues;

    /// <summary>
    /// Initializes a new instance of <see cref="ResponseSizeAlgebra"/> for
    /// the static/assumed path: a variable-bound slicing argument falls back
    /// to its schema-declared assumption instead of a coerced value.
    /// </summary>
    /// <param name="schemaIndex">
    /// The schema index used to resolve list-size metadata.
    /// </param>
    public ResponseSizeAlgebra(CostSchemaIndex schemaIndex)
    {
        ArgumentNullException.ThrowIfNull(schemaIndex);
        _schemaIndex = schemaIndex;
        _variableValues = null;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ResponseSizeAlgebra"/> that
    /// resolves a variable-bound slicing argument from
    /// <paramref name="variableValues"/>, the same coerced values the
    /// optimized <see cref="CostPlan"/> path receives at evaluation time.
    /// </summary>
    /// <param name="schemaIndex">
    /// The schema index used to resolve list-size metadata.
    /// </param>
    /// <param name="variableValues">
    /// The coerced variable values of the request.
    /// </param>
    [Experimental(CostExperiments.AnalysisAlgebra)]
    public ResponseSizeAlgebra(CostSchemaIndex schemaIndex, ICostVariableValues variableValues)
    {
        ArgumentNullException.ThrowIfNull(schemaIndex);
        ArgumentNullException.ThrowIfNull(variableValues);
        _schemaIndex = schemaIndex;
        _variableValues = variableValues;
    }

    /// <inheritdoc />
    public double Empty => ResponseSizeFieldRule.Empty;

    /// <inheritdoc />
    public double Field(in CollectedFieldGroup group, double child)
    {
        if (group.Field is not { } field || group.Member.ParentType is null || group.Member.Field is null)
        {
            return Empty;
        }

        var member = group.Member;
        return ResponseSizeFieldRule.Field(
            member.Field.Type,
            ResolveListMultiplier(
                member.ParentType.Name,
                member.Field.Name,
                member.Field,
                field.Arguments,
                group.InheritedSize),
            child);
    }

    /// <inheritdoc />
    public double Combine(double left, double right) => ResponseSizeFieldRule.Combine(left, right);

    /// <inheritdoc />
    public double Join(double left, double right) => ResponseSizeFieldRule.Join(left, right);

    /// <inheritdoc />
    public double Root(double rootTypeWeight, double selection) => selection;

    /// <summary>
    /// Resolves the list multiplier for one member's field call using the
    /// same list-size chain as the cost algebra.
    /// </summary>
    private double ResolveListMultiplier(
        string typeName,
        string fieldName,
        IOutputFieldDefinition field,
        IReadOnlyList<ArgumentNode> arguments,
        double? inheritedSize)
    {
        var metadata = _schemaIndex.GetListSizeMetadata(typeName, fieldName);
        var slicingArguments = SlicingArgumentValues.Build(metadata, field, arguments);
        ReadOnlySpan<double> inheritedSizes = inheritedSize is { } size ? [size] : [];

        return ListSizeResolver.Resolve(
            field.Type.IsListType(),
            metadata,
            inheritedSizes,
            slicingArguments,
            _variableValues,
            _schemaIndex.DefaultListSize);
    }
}
