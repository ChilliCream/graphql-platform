using System.Diagnostics.CodeAnalysis;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace GreenDonut.Data.Cursors.Serializers;

internal static class CompareToResolver
{
    private const string _compareTo = "CompareTo";

    public static CursorKeyCompareMethod GetCompareToMethod<[DynamicallyAccessedMembers(PublicMethods)] T>()
        => GetCompareToMethod(typeof(T));

    private static CursorKeyCompareMethod GetCompareToMethod([DynamicallyAccessedMembers(PublicMethods)] Type type)
        => new CursorKeyCompareMethod(type.GetMethod(_compareTo, [type])!, type);
}
