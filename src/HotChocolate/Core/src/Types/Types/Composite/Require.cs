using HotChocolate.Fusion.Language;

namespace HotChocolate.Types.Composite;

[DirectiveType(
    DirectiveNames.Lookup.Name,
    DirectiveLocation.ArgumentDefinition,
    IsRepeatable = false)]
public sealed class Require
{
    public Require(IValueSelectionNode field)
    {
        ArgumentNullException.ThrowIfNull(field);
        Field = field;
    }

    public IValueSelectionNode Field { get; set; }

    public override string ToString() => $"@require(field: \"{Field.ToString(false)}\")";
}
