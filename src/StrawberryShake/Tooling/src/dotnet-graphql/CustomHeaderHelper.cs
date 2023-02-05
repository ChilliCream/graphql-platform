namespace StrawberryShake.Tools;

public static class CustomHeaderHelper
{
    public static Dictionary<string, IEnumerable<string>> ParseHeadersArgument(
        IReadOnlyCollection<string?> arguments)
    {
        var headers = new Dictionary<string, IEnumerable<string>>();

        foreach (var argument in arguments)
        {
            var argumentParts = argument?.Trim().Split("=", 2);
            if (argumentParts?.Length != 2)
            {
                continue;
            }

            var argumentKey = argumentParts[0];

            var argumentValueParts = argumentParts[1].Trim().Split(",");

            _ = headers.TryAdd(argumentKey, argumentValueParts);
        }

        return headers;
    }
}
