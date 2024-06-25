using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers.Expressions.Parameters;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.ParameterExpressionBuilders;

internal sealed class HttpContextParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<HttpContext>(ctx => GlobalStateHelpers.GetHttpContext(ctx), isPure: true)
{
    public override ArgumentKind Kind => ArgumentKind.GlobalState;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(HttpContext);
}
