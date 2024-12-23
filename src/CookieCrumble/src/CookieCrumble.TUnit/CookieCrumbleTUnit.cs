namespace CookieCrumble.TUnit;

public sealed class CookieCrumbleTUnit : SnapshotModule
{
    protected override ITestFramework TryCreateTestFramework()
        => new TUnitFramework();
}
