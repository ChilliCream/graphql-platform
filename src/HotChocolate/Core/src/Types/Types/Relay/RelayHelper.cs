using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types.Relay
{
    internal static class RelayHelper
    {
        public static RelayOptions GetRelayOptions(
            this IDescriptorContext context)
        {
            if (context.ContextData.TryGetValue(typeof(RelayOptions).FullName!, out object? o) &&
                o is RelayOptions casted)
            {
                return casted;
            }

            return new RelayOptions();
        }

        public static ISchemaBuilder SetRelayOptions(
            this ISchemaBuilder schemaBuilder,
            RelayOptions options) =>
            schemaBuilder.SetContextData(typeof(RelayOptions).FullName!, options);
    }
}
