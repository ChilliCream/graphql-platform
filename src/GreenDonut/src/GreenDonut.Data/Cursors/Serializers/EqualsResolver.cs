using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace GreenDonut.Data.Cursors.Serializers;

internal static class EqualsResolver
{
    private const string _equals = "Equals";

    public static MethodInfo GetEqualsMethod<[DynamicallyAccessedMembers(PublicMethods)] T>()
        => GetEqualsMethod(typeof(T));

    private static MethodInfo GetEqualsMethod([DynamicallyAccessedMembers(PublicMethods)] Type type)
        => type.GetMethod(_equals, [type])!;
}
