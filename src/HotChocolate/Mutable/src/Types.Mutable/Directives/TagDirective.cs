using HotChocolate.Language;
using static HotChocolate.Types.Mutable.Properties.MutableResources;

namespace HotChocolate.Types.Mutable.Directives;

public sealed class TagDirective(string name)
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
