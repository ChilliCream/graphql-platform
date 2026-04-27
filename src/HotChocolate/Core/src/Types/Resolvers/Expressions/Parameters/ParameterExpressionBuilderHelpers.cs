using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal static class ParameterExpressionBuilderHelpers
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)]
    public static Type ContextType { get; } = typeof(IResolverContext);

    public static bool IsStateSetter(Type parameterType)
    {
        if (parameterType == typeof(SetState))
        {
            return true;
        }

        if (parameterType.IsGenericType
            && parameterType.GetGenericTypeDefinition() == typeof(SetState<>))
        {
            return true;
        }

        return false;
    }
}
