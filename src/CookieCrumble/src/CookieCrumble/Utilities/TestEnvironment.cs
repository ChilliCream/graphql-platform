using System.Diagnostics;

namespace CookieCrumble;

/// <summary>
/// Provides constant that help create specific snapshots for specific environments.
/// </summary>
public static class TestEnvironment
{
    // ReSharper disable InconsistentNaming
    public const string NET8_0 = "NET8_0";
    public const string NET9_0 = "NET9_0";
    public const string NET10_0 = "NET10_0";
    public const string NET11_0 = "NET11_0";
    // ReSharper restore InconsistentNaming

#if NET8_0
    /// <summary>
    /// The target framework identifier.
    /// </summary>
    public const string TargetFramework = NET8_0;
#elif NET9_0
    /// <summary>
    /// The target framework identifier.
    /// </summary>
    public const string TargetFramework = NET9_0;
#elif NET10_0
    /// <summary>
    /// The target framework identifier.
    /// </summary>
    public const string TargetFramework = NET10_0;
#elif NET11_0
    /// <summary>
    /// The target framework identifier.
    /// </summary>
    public const string TargetFramework = NET11_0;
#endif

    /// <summary>
    /// Returns a postfix for the snapshot name based on the target framework.
    /// </summary>
    /// <example>
    /// <c>Postfix(["NET8_0", "NET9_0"], ["NET10_0"])</c> will return <c>"NET8_0_NET9_0"</c> if the target framework is
    /// <c>NET8_0</c> or <c>NET9_0</c>, <c>"NET10_0"</c> if the target framework is <c>NET10_0</c>, or <c>null</c> if
    /// the target framework is <c>NET11_0</c>.
    /// </example>
    /// <param name="targetFrameworkGroups">
    /// A list of target framework groups. Each group is a list of target frameworks that should share the same snapshot
    /// postfix.
    /// </param>
    /// <returns>
    /// A postfix for the snapshot name based on the target framework, or null if the target framework does not match
    /// any group.
    /// </returns>
    public static string? Postfix(params string[][] targetFrameworkGroups)
    {
        foreach (var group in targetFrameworkGroups)
        {
            if (group.Contains(TargetFramework))
            {
                return string.Join("_", group);
            }
        }

        return null;
    }

    public static bool IsLocalEnvironment()
    {
        return !IsCIEnvironment();
    }

    public static bool IsCIEnvironment()
    {
        return bool.TryParse(
            Environment.GetEnvironmentVariable("CI_BUILD"),
            out var result)
            && result;
    }

    public static CancellationTokenSource CreateCancellationTokenSource(TimeSpan? timeSpan = null)
    {
        if (Debugger.IsAttached)
        {
            return new CancellationTokenSource();
        }

        return new CancellationTokenSource(timeSpan ?? TimeSpan.FromSeconds(10));
    }
}
