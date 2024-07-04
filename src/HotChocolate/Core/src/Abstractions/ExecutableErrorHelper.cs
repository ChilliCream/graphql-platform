namespace HotChocolate;

internal static class ExecutableErrorHelper
{
    public static IError SequenceContainsMoreThanOneElement()
        => new Error(
            "Sequence contains more than one element.",
            ErrorCodes.Data.MoreThanOneElement);
}
