using HotChocolate.Types;
using Json.Schema;

namespace HotChocolate.ModelContextProtocol.Extensions;

internal static class InputFieldExtensions
{
    public static JsonSchema ToJsonSchema(this InputField inputField)
    {
        var graphQLType = inputField.Type;
        var schemaBuilder =
            graphQLType.ToJsonSchemaBuilder(isOneOf: inputField.DeclaringType.IsOneOf);

        // Description.
        if (inputField.Description is not null)
        {
            schemaBuilder.Description(inputField.Description);
        }

        // Default value.
        if (inputField.DefaultValue is not null)
        {
            schemaBuilder.Default(inputField.DefaultValue.ToJsonNode(graphQLType));
        }

        return schemaBuilder.Build();
    }
}
