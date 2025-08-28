using HotChocolate.Features;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

internal static class TagHelper
{
    public static void ModifyOptions(IDescriptorContext context, Action<TagOptions> configure)
        => configure(context.Features.GetOrSet<TagOptions>());
}
