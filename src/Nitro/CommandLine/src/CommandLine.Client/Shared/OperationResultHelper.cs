using ChilliCream.Nitro.Client.Exceptions;
using StrawberryShake;

namespace ChilliCream.Nitro.Client;

internal static class OperationResultHelper
{
    public static TData EnsureData<TData>(IOperationResult<TData> result)
        where TData : class
    {
        // TODO: Can probably be improved
        // TODO: If there are auth related errors they should be thrown with a proper exception
        if (result.Errors is { Count: > 0 })
        {
            var message = string.Join(Environment.NewLine, result.Errors.Select(t => t.Message));

            throw new NitroClientException(
                $"Operation failed:{Environment.NewLine}{message}");
        }

        return result.Data
            ?? throw new NitroClientException(
                "Operation failed: no data was returned.");
    }
}
