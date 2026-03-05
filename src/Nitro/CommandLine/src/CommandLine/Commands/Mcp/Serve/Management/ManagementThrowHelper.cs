namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management;

internal static class ManagementThrowHelper
{
    public static InvalidOperationException WorkspaceNotAvailable()
    {
        return new InvalidOperationException(
            "No workspace available. Ensure you are logged in and have a workspace selected.");
    }

    public static InvalidOperationException ApiIdRequired()
    {
        return new InvalidOperationException(
            "API ID is required. Provide the 'apiId' parameter or configure a default API via --api-id.");
    }

    public static InvalidOperationException GraphQLOperationFailed(string operation, string details)
    {
        return new InvalidOperationException(
            $"GraphQL operation '{operation}' failed: {details}");
    }
}
