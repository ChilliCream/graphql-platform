using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions.Parameters;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.ParameterExpressionBuilders;

internal sealed class HttpContextParameterExpressionBuilder
    : LambdaParameterExpressionBuilder<IPureResolverContext, HttpContext>
{
    public HttpContextParameterExpressionBuilder()
        : base(ctx => GlobalStateHelpers.GetHttpContext(ctx)) { }

    public override ArgumentKind Kind => ArgumentKind.GlobalState;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(HttpContext);
}
