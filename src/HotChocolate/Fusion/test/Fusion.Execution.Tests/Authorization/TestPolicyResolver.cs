namespace HotChocolate.Fusion.Authorization;

internal static class TestPolicyResolver
{
    public static IPolicyResolver Create()
        => new PolicyResolver(new BuiltInPolicyProvider(), []);
}
