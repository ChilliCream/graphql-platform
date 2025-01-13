namespace CookieCrumble.Xunit;

public class CookieCrumbleXunit : SnapshotModule
{
    protected override ITestFramework TryCreateTestFramework()
        => new XunitFramework();
}
