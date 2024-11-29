#nullable enable
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

internal static class TagHelper
{
    public static void ModifyOptions(IDescriptorContext context, Action<TagOptions> configure)
    {
        TagOptions? options = null;

        if (context.ContextData.TryGetValue(WellKnownContextData.TagOptions, out var value) &&
            value is TagOptions opt)
        {
            options = opt;
        }

        options ??= new TagOptions();
        context.ContextData[WellKnownContextData.TagOptions] = options;
        configure(options);
    }
}
