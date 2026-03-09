namespace CookieCrumble;

public sealed class DisposableSnapshot(string? postFix = null, string? extension = null)
    : Snapshot(postFix, extension)
    , IDisposable
{
    public void Dispose() => Match();
}
