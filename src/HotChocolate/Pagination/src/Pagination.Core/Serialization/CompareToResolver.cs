using System.Reflection;

namespace HotChocolate.Pagination.Serialization;

internal static class CompareToResolver
{
    private const string _compareTo = "CompareTo";

    public static MethodInfo GetCompareToMethod<T>()
        => GetCompareToMethod(typeof(T));

    public static MethodInfo GetCompareToMethod(Type type)
        => type.GetMethod(_compareTo, [type])!;
}
