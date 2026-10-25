namespace ChilliCream.Nitro.Client;

public sealed class NitroClientGraphQLException(string errorMessage, string? code) : NitroClientException("")
{
    public string ErrorMessage { get; } = errorMessage;

    public string? Code { get; } = code;
}
