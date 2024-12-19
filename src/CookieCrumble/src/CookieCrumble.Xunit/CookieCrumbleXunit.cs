namespace CookieCrumble.Xunit;

public sealed class CookieCrumbleXunit : SnapshotModule
{
    protected override ITestFramework TryCreateTestFramework()
        => new XunitFramework();
}
