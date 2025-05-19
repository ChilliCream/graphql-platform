using System.Reflection;

namespace GreenDonut.Data.Cursors;

/// <summary>
/// Represents a method used to compare cursor keys.
/// </summary>
/// <param name="methodInfo">The <see cref="MethodInfo"/> for the comparison method.</param>
/// <param name="type">The <see cref="Type"/> that the method belongs to.</param>
public sealed class CursorKeyCompareMethod(
    MethodInfo methodInfo,
    Type type)
{
    /// <summary>
    /// Gets the <see cref="MethodInfo"/> for the comparison method.
    /// </summary>
    public MethodInfo MethodInfo { get; } = methodInfo;

    /// <summary>
    /// Gets the <see cref="Type"/> that the method belongs to.
    /// </summary>
    public Type Type { get; } = type;
}
