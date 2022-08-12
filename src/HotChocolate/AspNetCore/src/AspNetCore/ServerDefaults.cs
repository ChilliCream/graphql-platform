namespace HotChocolate.AspNetCore;

internal static class ServerDefaults
{
    public const int MaxAllowedRequestSize = 20 * 1000 * 1024;

    public const int InitialBufferSize = 1024 * 4;
}
