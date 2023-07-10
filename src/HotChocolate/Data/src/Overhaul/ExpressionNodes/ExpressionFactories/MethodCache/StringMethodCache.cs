using System.Linq;
using System.Reflection;

namespace HotChocolate.Data.ExpressionNodes;

public static class StringMethodCache
{
    private static MethodInfo GetStringMethod(string name, int numParams)
    {
        return typeof(string)
            .GetMethods()
            .Single(m => m.Name == name && m.GetParameters().Length == numParams);
    }
    
    public static readonly MethodInfo StartsWith
        = GetStringMethod(nameof(string.StartsWith), 1);
    
    public static readonly MethodInfo EndsWith
        = GetStringMethod(nameof(string.EndsWith), 1);
}
