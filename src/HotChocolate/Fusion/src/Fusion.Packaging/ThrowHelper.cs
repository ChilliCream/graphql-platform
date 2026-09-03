namespace HotChocolate.Fusion.Packaging;

internal static class ThrowHelper
{
    public static ArgumentException RegoPolicyPackageMustMatchName()
        => new(
            "The Rego policy package must be a single segment that matches the policy name.",
            "policy");

    public static InvalidOperationException SignatureMustBeRemovedBeforeCommit()
        => new("The archive contains a stale signature. Call RemoveSignatureAsync before committing changes.");
}
