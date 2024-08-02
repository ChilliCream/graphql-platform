using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Factories;

internal static class SchemaBuildingDirectiveHelper
{
    private const string _definitionStackKey = "HotChocolate.Schema.Building.DefinitionStack";

    public static Stack<IDefinition> GetOrCreateDefinitionStack(this IDescriptorContext context)
    {
        if (!context.ContextData.TryGetValue(_definitionStackKey, out var value) ||
            value is not Stack<IDefinition> stack)
        {
            stack = new Stack<IDefinition>();
            context.ContextData[_definitionStackKey] = stack;
        }

        return stack;
    }
}
