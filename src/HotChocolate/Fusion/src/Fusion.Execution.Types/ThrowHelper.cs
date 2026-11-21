using HotChocolate.Fusion.Types.Directives;

namespace HotChocolate.Fusion.Types;

internal static class ThrowHelper
{
    public static void EnsureNotSealed(bool completed)
    {
        if (completed)
        {
            throw new NotSupportedException(
                "The type definition is sealed and cannot be modified.");
        }
    }

    public static NotSupportedException TypeSystemMemberSealed()
        => new NotSupportedException(
            "The type system member is sealed and cannot be modified.");

    public static InvalidOperationException InvalidCompletionContext()
        => new("The context has an invalid state.");

    public static DirectiveParserException PolicyDirectiveArgumentNotSupported(string argumentName)
        => new($"The argument `{argumentName}` is not supported on @fusion__policy.");

    public static DirectiveParserException PolicyDirectiveGroupsInvalid(string argumentName)
        => new($"The `{argumentName}` argument of @fusion__policy must be a list of string lists.");
}
