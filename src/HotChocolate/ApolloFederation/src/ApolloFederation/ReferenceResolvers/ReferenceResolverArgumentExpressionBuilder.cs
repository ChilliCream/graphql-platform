using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Resolvers.Expressions.Parameters;
using HotChocolate.Types;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

namespace HotChocolate.ApolloFederation;

internal sealed class ReferenceResolverArgumentExpressionBuilder
    : ScopedStateParameterExpressionBuilder
{
    private readonly MethodInfo _getValue =
        typeof(ArgumentParser).GetMethod(
            nameof(ArgumentParser.GetValue),
            BindingFlags.Static | BindingFlags.Public)!;

    public override ArgumentKind Kind => ArgumentKind.LocalState;

    protected override PropertyInfo ContextDataProperty { get; } =
        ContextType.GetProperty(nameof(IResolverContext.LocalContextData))!;

    protected override MethodInfo SetStateMethod { get; } =
        typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.SetLocalState))!;

    protected override MethodInfo SetStateGenericMethod { get; } =
        typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.SetLocalStateGeneric))!;

    public override bool IsDefaultHandler => true;

    public IReadOnlyList<string[]> Paths { get; private set; } = Array.Empty<string[]>();

    public override bool CanHandle(ParameterInfo parameter) => true;

    protected override string GetKey(ParameterInfo parameter) => AnyType.DataField;

    public override Expression Build(ParameterInfo parameter, Expression context)
    {
        ConstantExpression path = Expression.Constant(GetPath(parameter), typeof(string[]));
        ConstantExpression key = Expression.Constant(AnyType.DataField, typeof(string));
        Expression value = BuildGetter(parameter, key, context, typeof(IValueNode));
        MethodInfo getValueMethod = _getValue.MakeGenericMethod(parameter.ParameterType);
        Expression<Func<IResolverContext, IObjectType>> getObjectTypeExpr = t => t.ObjectType;
        Expression getObjectType = Expression.Invoke(getObjectTypeExpr, context);
        Expression getValue = Expression.Call(getValueMethod, value, getObjectType, path);
        return getValue;
    }

    private string[] GetPath(ParameterInfo parameter)
    {
        var path = parameter.GetCustomAttribute<MapAttribute>() is { } attr
          ? attr.Path.Split('.')
          : new[] { parameter.Name! };

        if (Paths.Count == 0)
        {
            Paths = new string[][] { path };
        }
        else if (Paths is List<string[]> list)
        {
            list.Add(path);
        }

        return path;
    }
}
