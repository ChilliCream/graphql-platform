namespace CookieCrumble.Xunit3;

public sealed class CookieCrumbleXunit3 : SnapshotModule
{
    protected override ITestFramework TryCreateTestFramework()
        => new Xunit3Framework();
}
