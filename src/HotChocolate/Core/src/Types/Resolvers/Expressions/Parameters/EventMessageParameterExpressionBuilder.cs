using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using static HotChocolate.Properties.TypeResources;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class EventMessageParameterExpressionBuilder
    : LambdaParameterExpressionBuilder<IResolverContext, object>
{
    public EventMessageParameterExpressionBuilder()
        : base(ctx => GetEventMessage(ctx.ScopedContextData))
    {
    }

    public override ArgumentKind Kind => ArgumentKind.EventMessage;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(EventMessageAttribute));

    public override Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Convert(base.Build(context), context.Parameter.ParameterType);

    private static object GetEventMessage(IImmutableDictionary<string, object?> contextData)
    {
        if (!contextData.TryGetValue(WellKnownContextData.EventMessage, out var message) ||
            message is null)
        {
            throw new InvalidOperationException(
                EventMessageParameterExpressionBuilder_MessageNotFound);
        }
        return message;
    }
}
