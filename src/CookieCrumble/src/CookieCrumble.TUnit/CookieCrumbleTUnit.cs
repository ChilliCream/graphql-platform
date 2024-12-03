namespace CookieCrumble.TUnit;

public static class CookieCrumbleTUnit
{
    public static void Initialize()
    {
        Snapshot.RegisterTestFramework(new TUnitFramework());
    }
}
