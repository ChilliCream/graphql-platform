namespace HotChocolate.Fusion.Packaging;

internal static class ThrowHelper
{
    public static InvalidOperationException SignatureMustBeRemovedBeforeCommit()
        => new("The archive contains a stale signature. Call RemoveSignatureAsync before committing changes.");
}
