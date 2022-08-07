namespace CookieCrumble;

/// <summary>
/// Provides constant that help create specific snapshots for specific environments.
/// </summary>
public static class TestEnvironment
{
#if NETCOREAPP3_1
    /// <summary>
    /// The target framework identifier.
    /// </summary>
    public const string TargetFramework = "NETCOREAPP3_1";
#elif NET5_0
    /// <summary>
    /// The target framework identifier.
    /// </summary>
    public const string TargetFramework = "NET5_0";
#elif NET6_0
    /// <summary>
    /// The target framework identifier.
    /// </summary>
    public const string TargetFramework = "NET6_0";
#elif NET7_0
    /// <summary>
    /// The target framework identifier.
    /// </summary>
    public const string TargetFramework = "NET7_0";
#endif
}
