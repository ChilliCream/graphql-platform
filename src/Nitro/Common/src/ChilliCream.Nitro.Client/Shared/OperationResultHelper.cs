using System.Net;
using System.Text.RegularExpressions;
using StrawberryShake;

namespace ChilliCream.Nitro.Client;

internal static partial class OperationResultHelper
{
    [GeneratedRegex(@"Response status code does not indicate success: (\d{3})")]
    private static partial Regex StatusCodeRegex();

    public static TData EnsureData<TData>(IOperationResult<TData> result)
        where TData : class
    {
        if (result.Errors is { Count: > 0 } errors)
        {
            if (errors.Any(e => e.Code is "AUTH_NOT_AUTHENTICATED" or "AUTH_NOT_AUTHORIZED"))
            {
                throw new NitroClientAuthorizationException();
            }

            var httpRequestException = result.Errors
                .Select(e => e.Exception)
                .OfType<HttpRequestException>()
                .FirstOrDefault();

            if (errors.Count == 1 && httpRequestException is not null)
            {
                throw new NitroClientHttpRequestException(httpRequestException.StatusCode);
            }

            var firstError = errors[0];

            var statusCodeMatch = StatusCodeRegex().Match(firstError.Message);
            if (statusCodeMatch.Success
                && int.TryParse(statusCodeMatch.Groups[1].ValueSpan, out var statusCodeValue)
                && Enum.IsDefined(typeof(HttpStatusCode), statusCodeValue))
            {
                throw new NitroClientHttpRequestException((HttpStatusCode)statusCodeValue);
            }

            throw new NitroClientGraphQLException(firstError.Message, firstError.Code);
        }

        return result.Data ?? throw new NitroClientGraphQLException("No data was returned.", null);
    }
}
