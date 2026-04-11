using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers;

/// <summary>
/// Represents a type discovered from a <c>[JsonSerializable(typeof(T))]</c> attribute on a
/// <c>JsonSerializerContext</c>, including its type hierarchy for context-only message resolution.
/// </summary>
/// <param name="TypeName">The fully qualified type name of the serializable type.</param>
/// <param name="TypeHierarchy">
/// The unfiltered type hierarchy (base types excluding <c>object</c>, plus all interfaces),
/// as fully qualified display strings.
/// </param>
/// <param name="AttributeLocation">
/// The equatable source location of the <c>[JsonSerializable]</c> attribute, or <see langword="null"/> if unavailable.
/// </param>
public sealed record JsonSerializableTypeInfo(
    string TypeName,
    ImmutableEquatableArray<string> TypeHierarchy,
    LocationInfo? AttributeLocation) : IEquatable<JsonSerializableTypeInfo>;
