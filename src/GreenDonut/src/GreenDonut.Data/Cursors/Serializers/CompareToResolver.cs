using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace GreenDonut.Data.Cursors.Serializers;

internal static class CompareToResolver
{
    private const string CompareTo = "CompareTo";

    public static MethodInfo GetCompareToMethod<[DynamicallyAccessedMembers(PublicMethods)] T>()
        => GetCompareToMethod(typeof(T));

    private static MethodInfo GetCompareToMethod([DynamicallyAccessedMembers(PublicMethods)] Type type)
        => type.GetMethod(CompareTo, [type])!;
}
