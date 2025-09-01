namespace Demo.Gateway.Properties;

public static class Env
{
    public const string GatewayApi = "gateway-api";

    public static string Version => typeof(Env).Assembly.GetName().Version?.ToString() ?? "0.0.0";
}
