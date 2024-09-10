using System.Reflection;

namespace HotChocolate.Resolvers.Expressions;

internal static class TypeInfoExtensions
{
    public static MethodInfo GetDeclaredMethod(
        this TypeInfo typeInfo,
        string name,
        params Type[] types)
    {
        return typeInfo.GetDeclaredMethods(name).FirstOrDefault(t =>
        {
            var parameters = t.GetParameters();
            if (types.Length != parameters.Length)
            {
                return false;
            }

            for (var i = 0; i < types.Length; i++)
            {
                if (types[i] != parameters[i].ParameterType)
                {
                    return false;
                }
            }

            return true;
        });
    }
}
