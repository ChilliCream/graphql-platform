#nullable enable

namespace HotChocolate.Types;

internal class InputContextData
{
    public static readonly string Input = "HotChocolate.Types.InputAttribute";

    public InputContextData(string? typeName, string argumentName)
    {
        TypeName = typeName;
        ArgumentName = argumentName;
    }

    public string? TypeName { get; }

    public string ArgumentName { get; }
}
