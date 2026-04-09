using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers;

/// <summary>
/// Carries the complete set of type names declared as <c>[JsonSerializable]</c> on the
/// module's <c>JsonSerializerContext</c>. Used by downstream generators to determine which
/// types have local serializer support and should receive <c>AddMessageConfiguration</c>
/// registrations with pre-built serializers.
/// </summary>
/// <param name="TypeNames">
/// The fully qualified type names of all types declared via <c>[JsonSerializable(typeof(T))]</c>
/// on the module's <c>JsonSerializerContext</c>.
/// </param>
public sealed record JsonContextSerializableTypesInfo(
    ImmutableEquatableArray<string> TypeNames) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => "JsonContextTypes";
}
