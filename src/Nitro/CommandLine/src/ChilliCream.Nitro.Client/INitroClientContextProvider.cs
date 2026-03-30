namespace ChilliCream.Nitro.Client;

public interface INitroClientContextProvider
{
    Uri Url { get; }

    INitroClientAuthorization? Authorization { get; }
}
