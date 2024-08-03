using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace HotChocolate.Pagination.Serialization;

internal static class CompareToResolver
{
    private const string _compareTo = "CompareTo";

    public static MethodInfo GetCompareToMethod<[DynamicallyAccessedMembers(PublicMethods)] T>()
        => GetCompareToMethod(typeof(T));

    private static MethodInfo GetCompareToMethod([DynamicallyAccessedMembers(PublicMethods)] Type type)
        => type.GetMethod(_compareTo, [type])!;
}
