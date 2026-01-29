using System.Diagnostics.CodeAnalysis;

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
}
