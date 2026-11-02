using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Directives;

internal sealed class TagDirective(string name)
{
    public string Name { get; set; } = name;

    public static TagDirective From(IDirective directive)
    {
        if (!directive.Arguments.TryGetValue(DirectiveNames.Tag.Arguments.Name, out var nameArg)
            || nameArg is not StringValueNode name)
        {
            throw new InvalidOperationException(TagDirective_NameArgument_Invalid);
        }

        return new TagDirective(name.Value);
    }
}
