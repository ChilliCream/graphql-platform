using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// The built-in maximum response-size analysis algebra.
/// </summary>
public sealed class ResponseSizeAlgebra : IAnalysisAlgebra<double>
{
    private readonly CostSchemaSnapshot _snapshot;

    /// <summary>
    /// Initializes a new instance of <see cref="ResponseSizeAlgebra"/>.
    /// </summary>
    /// <param name="snapshot">
    /// The schema snapshot used to resolve list-size metadata.
    /// </param>
    public ResponseSizeAlgebra(CostSchemaSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _snapshot = snapshot;
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
            ResolveListMultiplier(member.ParentType.Name, member.Field.Name, member.Field, field.Arguments, group.InheritedSize),
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
        var metadata = _snapshot.GetListSizeMetadata(typeName, fieldName);
        var slicingArguments = SlicingArgumentValues.Build(metadata, field, arguments);
        ReadOnlySpan<double> inheritedSizes = inheritedSize is { } size ? [size] : [];

        return ListSizeResolver.Resolve(
            field.Type.IsListType(),
            metadata,
            inheritedSizes,
            slicingArguments,
            variableValues: null,
            _snapshot.Options.DefaultListSize);
    }
}
