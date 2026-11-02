namespace HotChocolate.Fusion.Policies;

internal static class TestPolicyResolver
{
    public static IPolicyResolver Create()
        => new PolicyResolver(new BuiltInPolicyProvider(), []);
}
