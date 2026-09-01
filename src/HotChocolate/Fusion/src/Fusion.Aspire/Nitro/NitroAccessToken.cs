using System.Text.Json;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal static class NitroAccessToken
{
    private const string ApiUrlClaim = "api_url";
    private const string AudienceClaim = "aud";

    public static bool TryGetApiUrl(string accessToken, out string? apiUrl)
    {
        ArgumentNullException.ThrowIfNull(accessToken);

        apiUrl = null;
        if (!TryReadPayload(accessToken, out var document))
        {
            return false;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty(ApiUrlClaim, out var claim)
                || claim.ValueKind is not JsonValueKind.String)
            {
                return false;
            }

            apiUrl = claim.GetString();
            return !string.IsNullOrWhiteSpace(apiUrl);
        }
    }

    public static bool TryGetAudience(string identityToken, out string? audience)
    {
        ArgumentNullException.ThrowIfNull(identityToken);

        audience = null;
        if (!TryReadPayload(identityToken, out var document))
        {
            return false;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty(AudienceClaim, out var claim))
            {
                return false;
            }

            if (claim.ValueKind is JsonValueKind.String)
            {
                audience = claim.GetString();
                return !string.IsNullOrWhiteSpace(audience);
            }

            if (claim.ValueKind is JsonValueKind.Array
                && claim.GetArrayLength() is 1
                && claim[0].ValueKind is JsonValueKind.String)
            {
                audience = claim[0].GetString();
                return !string.IsNullOrWhiteSpace(audience);
            }

            return false;
        }
    }

    private static bool TryReadPayload(string token, out JsonDocument document)
    {
        document = null!;
        var firstSeparator = token.IndexOf('.');

        if (firstSeparator < 0)
        {
            return false;
        }

        var payloadStart = firstSeparator + 1;
        var secondSeparator = token.IndexOf('.', payloadStart);

        if (secondSeparator <= payloadStart)
        {
            return false;
        }

        var payload = token.AsSpan(payloadStart, secondSeparator - payloadStart);
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
            document = JsonDocument.Parse(decoded[..bytesWritten].ToArray());
            if (document.RootElement.ValueKind is not JsonValueKind.Object)
            {
                document.Dispose();
                document = null!;
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
