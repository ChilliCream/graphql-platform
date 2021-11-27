namespace HotChocolate.Types.Errors;

internal static class ErrorContextDataExtensions
{
    private const string _key = "HotChocolate.Types.Errors.IsErrorType";

    public static ExtensionData MarkAsError(this ExtensionData extensionData)
    {
        extensionData[_key] = true;
        return extensionData;
    }

    public static bool IsError(this ExtensionData extensionData) => extensionData.ContainsKey(_key);
}
