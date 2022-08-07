using System.Globalization;
using System.Reflection;

namespace CookieCrumble;

/// <summary>
/// The method base extension is used to add more functionality
/// to the class <see cref="MethodBase"/>
/// </summary>
internal static class MethodBaseExtension
{
    /// <summary>
    /// Creates the name of the method with class name.
    /// </summary>
    /// <param name="methodBase">The used method name to get the name.</param>
    public static string ToName(this MethodBase methodBase)
        => string.Concat(
            methodBase.ReflectedType!.Name.ToString(CultureInfo.InvariantCulture), ".",
            methodBase.Name.ToString(CultureInfo.InvariantCulture), ".snap");
}
