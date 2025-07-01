using HotChocolate.Features;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types.Relay;

internal static class RelayHelper
{
    public static MutationPayloadOptions GetMutationPayloadOptions(
        this IDescriptorContext context)
        => context.Features.GetOrSet<MutationPayloadOptions>();

    public static ISchemaBuilder ModifyMutationPayloadOptions(
        this ISchemaBuilder schemaBuilder,
        Action<MutationPayloadOptions>? configure = null)
    {
        configure?.Invoke(schemaBuilder.Features.GetOrSet<MutationPayloadOptions>());
        return schemaBuilder;
    }
}
