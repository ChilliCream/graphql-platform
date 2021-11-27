using static HotChocolate.Types.Errors.ErrorContextData;

namespace HotChocolate.Types.Errors;

internal static class ErrorContextDataExtensions
{
    public static ExtensionData MarkAsError(this ExtensionData extensionData)
    {
        extensionData[IsErrorType] = true;
        return extensionData;
    }

    public static bool IsError(this ExtensionData extensionData) =>
        extensionData.ContainsKey(IsErrorType);
}
