using HotChocolate.Language;

namespace HotChocolate.Fusion.Composition;

internal sealed class RequireDirective
{
    public RequireDirective(FieldNode field)
    {
        Field = field;
    }

    public FieldNode Field { get; }
}
