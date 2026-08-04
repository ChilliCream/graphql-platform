namespace HotChocolate.Fusion.Planning;

/// <summary>
/// A segment of a field path, identifying a field by its name and optional alias.
/// <see cref="TypeCondition"/> carries the name of the type condition that was
/// active when a composite field was entered, so the incremental plan operation
/// can re-wrap that field in an <c>... on Type</c> inline fragment when its
/// enclosing field returns an abstract type.
/// </summary>
internal readonly record struct FieldPathSegment(string FieldName, string? Alias, string? TypeCondition)
{
    public string ResponseName => Alias ?? FieldName;
}
