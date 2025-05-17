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

    public static void TypeSystemMemberSealed()
    {
        throw new NotSupportedException(
            "The type system member is sealed and cannot be modified.");
    }
}
