/*
 * This code was copied from xunit samples https://github.com/xunit/samples.xunit/blob/main/v3/UseCultureExample/UseCultureAttribute.cs
 */

using System.Globalization;
using System.Reflection;
using Xunit.v3;

namespace CookieCrumble.Xunit3.Attributes;

/// <summary>
/// Apply this attribute to a test class or method to replace the current thread's
/// <see cref="CultureInfo.CurrentCulture" /> and <see cref="CultureInfo.CurrentUICulture" />
/// with a specific culture while the test runs, restoring the originals afterward.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class UseCultureAttribute : BeforeAfterTestAttribute
{
    private readonly Lazy<CultureInfo> _culture;
    private readonly Lazy<CultureInfo> _uiCulture;
    private CultureInfo _originalCulture = null!;
    private CultureInfo _originalUiCulture = null!;

    /// <summary>
    /// Initializes the attribute to use <paramref name="culture" /> as both the
    /// <see cref="Culture" /> and <see cref="UICulture" />.
    /// </summary>
    /// <param name="culture">The name of the culture.</param>
    public UseCultureAttribute(string culture)
        : this(culture, culture) { }

    /// <summary>
    /// Initializes the attribute to use <paramref name="culture" /> as the <see cref="Culture" />
    /// and <paramref name="uiCulture" /> as the <see cref="UICulture" />.
    /// </summary>
    /// <param name="culture">The name of the culture.</param>
    /// <param name="uiCulture">The name of the UI culture.</param>
    public UseCultureAttribute(string culture, string uiCulture)
    {
        _culture = new Lazy<CultureInfo>(() => new CultureInfo(culture, false));
        _uiCulture = new Lazy<CultureInfo>(() => new CultureInfo(uiCulture, false));
    }

    /// <summary>
    /// Gets the culture.
    /// </summary>
    public CultureInfo Culture { get { return _culture.Value; } }

    /// <summary>
    /// Gets the UI culture.
    /// </summary>
    public CultureInfo UICulture { get { return _uiCulture.Value; } }

    /// <summary>
    /// Captures the current thread's <see cref="CultureInfo.CurrentCulture" /> and
    /// <see cref="CultureInfo.CurrentUICulture" />, then replaces them with
    /// <see cref="Culture" /> and <see cref="UICulture" />.
    /// </summary>
    /// <param name="methodUnderTest">The method under test.</param>
    /// <param name="test">The current test.</param>
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        _originalCulture = Thread.CurrentThread.CurrentCulture;
        _originalUiCulture = Thread.CurrentThread.CurrentUICulture;

        Thread.CurrentThread.CurrentCulture = Culture;
        Thread.CurrentThread.CurrentUICulture = UICulture;

        CultureInfo.CurrentCulture.ClearCachedData();
        CultureInfo.CurrentUICulture.ClearCachedData();
    }

    /// <summary>
    /// Restores the current thread's <see cref="CultureInfo.CurrentCulture" /> and
    /// <see cref="CultureInfo.CurrentUICulture" /> to the values captured before the test ran.
    /// </summary>
    /// <param name="methodUnderTest">The method under test.</param>
    /// <param name="test">The current test.</param>
    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        Thread.CurrentThread.CurrentCulture = _originalCulture;
        Thread.CurrentThread.CurrentUICulture = _originalUiCulture;

        CultureInfo.CurrentCulture.ClearCachedData();
        CultureInfo.CurrentUICulture.ClearCachedData();
    }
}
