namespace HotChocolate.Adapters.OpenApi;

internal static class OpenApiHelpers
{
    public static string GetOperationId(string operationName)
    {
        if (char.IsLower(operationName[0]))
        {
            return operationName;
        }

        return string.Create(operationName.Length, operationName, (span, str) =>
        {
            str.AsSpan().CopyTo(span);
            span[0] = char.ToLowerInvariant(span[0]);
        });
    }
}
