namespace HotChocolate.Fusion;

/// <summary>
/// Represents a <em>source schema</em> (Composite Schemas §2) identified by a name
/// and its SDL as raw text (unparsed).
/// See: https://graphql.github.io/composite-schemas-spec/draft/#2
/// </summary>
public readonly record struct SourceSchemaText(
    string Name,
    string SourceText);
