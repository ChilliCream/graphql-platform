namespace HotChocolate.Fusion.Planning;

/// <summary>
/// A segment of a field path, identifying a field by its name and optional alias.
/// </summary>
internal readonly record struct FieldPathSegment(string FieldName, string? Alias)
{
    public string ResponseName => Alias ?? FieldName;
}
