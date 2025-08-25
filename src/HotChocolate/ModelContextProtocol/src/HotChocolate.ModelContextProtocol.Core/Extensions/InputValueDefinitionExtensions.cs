using HotChocolate.Types;
using Json.Schema;

namespace HotChocolate.ModelContextProtocol.Extensions;

internal static class InputValueDefinitionExtensions
{
    public static JsonSchema ToJsonSchema(this IInputValueDefinition inputField)
    {
        var type = inputField.Type;
        var schemaBuilder = type.ToJsonSchemaBuilder(isOneOf: inputField.DeclaringType.IsOneOf);

        // Description.
        if (inputField.Description is not null)
        {
            schemaBuilder.Description(inputField.Description);
        }

        // Default value.
        if (inputField.DefaultValue is not null)
        {
            schemaBuilder.Default(inputField.DefaultValue.ToJsonNode(type));
        }

        return schemaBuilder.Build();
    }
}
