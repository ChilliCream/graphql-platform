using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers;

/// <summary>
/// Pre-extracted, equatable information about the <c>JsonSerializerContext</c> referenced
/// from a <c>[MessagingModule]</c> attribute. This model flows through the incremental
/// pipeline in place of <see cref="Microsoft.CodeAnalysis.Compilation"/> to avoid
/// re-execution on every keystroke.
/// </summary>
/// <param name="JsonContextTypeName">
/// The fully qualified type name of the <c>JsonSerializerContext</c>, or <see langword="null"/> if not specified.
/// </param>
/// <param name="SerializableTypes">
/// The types declared via <c>[JsonSerializable(typeof(T))]</c> on the context, including their
/// type hierarchies for context-only message resolution.
/// </param>
public sealed record JsonContextInfo(
    string? JsonContextTypeName,
    ImmutableEquatableArray<JsonSerializableTypeInfo> SerializableTypes);
