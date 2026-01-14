using HotChocolate.Features;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Factories;

internal static class SchemaBuildingDirectiveHelper
{
    public static Stack<ITypeSystemConfiguration> GetOrCreateConfigurationStack(
        this IDescriptorContext context)
        => context.Features.GetOrSet<ConfigurationFeature>().Configurations;

    private sealed class ConfigurationFeature
    {
        public Stack<ITypeSystemConfiguration> Configurations { get; } = [];
    }
}
