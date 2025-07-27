using HotChocolate.Language;

namespace HotChocolate.Types.Composite;

public sealed class Key
{
    public Key(SelectionSetNode fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Fields = fields;
    }

    public SelectionSetNode Fields { get; }

    public override string ToString()
    {
        var fields = Fields.ToString(false);
        fields = fields.Substring(1, fields.Length - 2);
        return $"@require(fields: {fields})";
    }
}
