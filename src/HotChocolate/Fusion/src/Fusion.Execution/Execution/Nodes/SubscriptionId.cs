namespace HotChocolate.Fusion.Execution.Nodes;

internal static class SubscriptionId
{
    private static ulong s_subscriptionId;

    public static ulong Next() => Interlocked.Increment(ref s_subscriptionId);
}
