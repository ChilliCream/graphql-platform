using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types.Relay;

internal static class RelayHelper
{
    public static MutationPayloadOptions GetMutationPayloadOptions(
        this IDescriptorContext context)
    {
        if (context.ContextData.TryGetValue(typeof(MutationPayloadOptions).FullName!, out var o) &&
            o is MutationPayloadOptions casted)
        {
            return casted;
        }

        return new MutationPayloadOptions();
    }

    public static ISchemaBuilder SetMutationPayloadOptions(
        this ISchemaBuilder schemaBuilder,
        MutationPayloadOptions options) =>
        schemaBuilder.SetContextData(typeof(MutationPayloadOptions).FullName!, options);
}
