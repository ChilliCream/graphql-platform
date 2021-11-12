namespace StrawberryShake.Tools.OAuth;

public class AccessToken
{
    public AccessToken(string token, string? scheme)
    {
        Token = token;
        Scheme = scheme;
    }

    public string Token { get; }

    public string? Scheme { get; }
}
