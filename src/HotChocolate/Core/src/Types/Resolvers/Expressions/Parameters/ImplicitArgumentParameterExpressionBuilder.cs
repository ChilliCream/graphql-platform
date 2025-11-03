using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal class ImplicitArgumentParameterExpressionBuilder
    : ArgumentParameterExpressionBuilder
{
    public override bool CanHandle(ParameterInfo parameter)
        => true;
}
