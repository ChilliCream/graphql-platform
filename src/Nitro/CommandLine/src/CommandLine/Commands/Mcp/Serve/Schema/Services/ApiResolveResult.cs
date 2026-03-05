namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

internal sealed class ApiResolveResult
{
    public bool IsSuccess { get; private init; }
    public string ApiId { get; private init; } = string.Empty;
    public string ApiName { get; private init; } = string.Empty;
    public string? ErrorMessage { get; private init; }

    public static ApiResolveResult Success(string apiId, string apiName)
        => new()
        {
            IsSuccess = true,
            ApiId = apiId,
            ApiName = apiName
        };

    public static ApiResolveResult Error(string message) => new() { IsSuccess = false, ErrorMessage = message };
}
