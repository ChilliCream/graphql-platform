using StrawberryShake;
using static ChilliCream.Nitro.CLI.ThrowHelper;

namespace ChilliCream.Nitro.CLI.Helpers;

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
