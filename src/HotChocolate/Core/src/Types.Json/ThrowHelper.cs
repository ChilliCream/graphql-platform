namespace HotChocolate.Types;

internal static class ThrowHelper
{
    public static SchemaException CannotInferTypeFromJsonObj(string typeName)
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "Cannot not infer the correct mapping for the JSON object type `{0}`.",
                    typeName)
                .Build());
}
