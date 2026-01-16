using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Helpers;

public static class StrawberryShakeExtensions
{
    public static T EnsureData<T>(this IOperationResult<T> result) where T : class
    {
        if (result.Data is not { } data)
        {
            if (result.Errors.FirstOrDefault() is { } error)
            {
                throw ThereWasAnIssueWithTheRequest(error.Message);
            }

            throw ThereWasAnIssueWithTheRequest();
        }

        return data;
    }
}
