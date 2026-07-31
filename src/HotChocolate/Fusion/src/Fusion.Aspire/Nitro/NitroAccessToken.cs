using System.Text.Json;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal static class NitroAccessToken
{
    private const string ApiUrlClaim = "api_url";

    public static bool TryGetApiUrl(string accessToken, out string? apiUrl)
    {
        ArgumentNullException.ThrowIfNull(accessToken);

        apiUrl = null;
        var firstSeparator = accessToken.IndexOf('.');

        if (firstSeparator < 0)
        {
            return false;
        }

        var payloadStart = firstSeparator + 1;
        var secondSeparator = accessToken.IndexOf('.', payloadStart);

        if (secondSeparator <= payloadStart)
        {
            return false;
        }

        var payload = accessToken.AsSpan(payloadStart, secondSeparator - payloadStart);
        var padding = (4 - payload.Length % 4) % 4;
        Span<char> base64 = payload.Length + padding <= 1024
            ? stackalloc char[payload.Length + padding]
            : new char[payload.Length + padding];

        for (var i = 0; i < payload.Length; i++)
        {
            base64[i] = payload[i] switch
            {
                '-' => '+',
                '_' => '/',
                var character => character
            };
        }

        base64[payload.Length..].Fill('=');

        var decodedLength = (base64.Length * 3 + 3) / 4;
        Span<byte> decoded = decodedLength <= 1024
            ? stackalloc byte[decodedLength]
            : new byte[decodedLength];

        if (!Convert.TryFromBase64Chars(base64, decoded, out var bytesWritten))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(decoded[..bytesWritten].ToArray());

            if (document.RootElement.ValueKind is not JsonValueKind.Object
                || !document.RootElement.TryGetProperty(ApiUrlClaim, out var claim)
                || claim.ValueKind is not JsonValueKind.String)
            {
                return false;
            }

            apiUrl = claim.GetString();
            return !string.IsNullOrWhiteSpace(apiUrl);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
