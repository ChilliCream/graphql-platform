using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions.Parameters;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.ParameterExpressionBuilders;

internal sealed class HttpRequestParameterExpressionBuilder
    : LambdaParameterExpressionBuilder<IPureResolverContext, HttpRequest>
{
    public HttpRequestParameterExpressionBuilder()
        : base(ctx => GlobalStateHelpers.GetHttpRequest(ctx)) { }

    public override ArgumentKind Kind => ArgumentKind.GlobalState;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(HttpRequest);
}

