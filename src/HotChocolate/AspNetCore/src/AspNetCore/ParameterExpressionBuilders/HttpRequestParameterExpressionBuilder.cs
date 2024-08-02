using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers.Expressions.Parameters;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.ParameterExpressionBuilders;

internal sealed class HttpRequestParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<HttpRequest>(ctx => GlobalStateHelpers.GetHttpRequest(ctx), isPure: true)
{
    public override ArgumentKind Kind => ArgumentKind.GlobalState;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(HttpRequest);
}
