using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Factories;

internal static class SchemaBuildingDirectiveHelper
{
    private const string _configurationStackKey = "HotChocolate.Schema.Building.ConfigurationStack";

    public static Stack<ITypeSystemConfiguration> GetOrCreateConfigurationStack(this IDescriptorContext context)
    {
        if (!context.ContextData.TryGetValue(_configurationStackKey, out var value) ||
            value is not Stack<ITypeSystemConfiguration> stack)
        {
            stack = new Stack<ITypeSystemConfiguration>();
            context.ContextData[_configurationStackKey] = stack;
        }

        return stack;
    }
}
