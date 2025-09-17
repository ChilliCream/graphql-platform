using HotChocolate.Language;

namespace HotChocolate.Types;

internal sealed class IsSelectedPattern(ObjectType type, string fieldName, SelectionSetNode pattern)
{
    public ObjectType Type { get; } = type;
    public string FieldName { get; } = fieldName;
    public SelectionSetNode Pattern { get; } = pattern;
}
