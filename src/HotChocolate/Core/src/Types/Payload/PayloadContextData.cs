#nullable enable

namespace HotChocolate.Types;

internal class PayloadContextData
{
    public static readonly string Payload = "HotChocolate.Types.PayloadAttribute";

    public PayloadContextData(string? fieldName, string? typeName)
    {
        FieldName = fieldName;
        TypeName = typeName;
    }

    public string? FieldName { get; }

    public string? TypeName { get; }
}
