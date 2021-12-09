using static HotChocolate.Types.ErrorContextDataKeys;

namespace HotChocolate.Types;

internal static class ErrorContextDataExtensions
{
    public static ExtensionData MarkAsError(this ExtensionData extensionData)
    {
        extensionData[IsErrorType] = true;
        return extensionData;
    }

    public static bool IsError(this ExtensionData extensionData)
        => extensionData.ContainsKey(IsErrorType);
}
