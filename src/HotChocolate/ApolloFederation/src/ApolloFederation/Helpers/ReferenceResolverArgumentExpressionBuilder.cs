using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Resolvers.Expressions.Parameters;
using static HotChocolate.ApolloFederation.Constants.WellKnownContextData;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

namespace HotChocolate.ApolloFederation.Helpers;

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

    public IReadOnlyList<string[]> Required { get; private set; } = Array.Empty<string[]>();

    public override bool CanHandle(ParameterInfo parameter) => true;

    protected override string GetKey(ParameterInfo parameter) => DataField;

    public override Expression Build(ParameterInfo parameter, Expression context)
    {
        var path = Expression.Constant(GetPath(parameter), typeof(string[]));
        var dataKey = Expression.Constant(DataField, typeof(string));
        var typeKey = Expression.Constant(TypeField, typeof(string));
        var value = BuildGetter(parameter, dataKey, context, typeof(IValueNode));
        var objectType = BuildGetter(parameter, typeKey, context, typeof(ObjectType));
        var getValueMethod = _getValue.MakeGenericMethod(parameter.ParameterType);
        Expression getValue = Expression.Call(getValueMethod, value, objectType, path);
        return getValue;
    }

    private string[] GetPath(ParameterInfo parameter)
    {
        var path = parameter.GetCustomAttribute<MapAttribute>() is { } attr
          ? attr.Path.Split('.')
          : new[] { parameter.Name! };

        if (Required.Count == 0)
        {
            Required = new string[][] { path };
        }
        else if (Required.Count == 1)
        {
            var required = new List<string[]>(Required);
            required.Add(path);
            Required = required;
        }
        else if (Required is List<string[]> list)
        {
            list.Add(path);
        }

        return path;
    }
}
