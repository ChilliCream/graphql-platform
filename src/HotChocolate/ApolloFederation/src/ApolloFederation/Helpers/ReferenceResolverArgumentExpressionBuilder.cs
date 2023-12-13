using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers.Expressions.Parameters;
using ApolloContextData = HotChocolate.ApolloFederation.Constants.WellKnownContextData;

namespace HotChocolate.ApolloFederation.Helpers;

internal sealed class ReferenceResolverArgumentExpressionBuilder :
    LocalStateParameterExpressionBuilder
{
    private readonly MethodInfo _getValue =
        typeof(ArgumentParser).GetMethod(
            nameof(ArgumentParser.GetValue),
            BindingFlags.Static | BindingFlags.Public)!;

    public override Expression Build(ParameterExpressionBuilderContext context)
    {
        var param = context.Parameter;
        var path = Expression.Constant(
            RequirePathAndGetSeparatedPath(param),
            typeof(string[]));
        var dataKey = Expression.Constant(
            ApolloContextData.DataField,
            typeof(string));
        var typeKey = Expression.Constant(
            ApolloContextData.TypeField,
            typeof(string));
        var value = BuildGetter(
            param,
            dataKey,
            context.ResolverContext,
            typeof(IValueNode));
        var objectType = BuildGetter(
            param,
            typeKey,
            context.ResolverContext,
            typeof(ObjectType));
        var getValueMethod = _getValue.MakeGenericMethod(param.ParameterType);
        var getValue = Expression.Call(
            getValueMethod,
            value,
            objectType,
            path);
        return getValue;
    }

    // NOTE: It will use the default handler without these two.
    public override bool IsDefaultHandler => true;
    public override bool CanHandle(ParameterInfo parameter) => true;

    private string[] RequirePathAndGetSeparatedPath(ParameterInfo parameter)
    {
        var path = parameter.GetCustomAttribute<MapAttribute>() is { } attr
            ? attr.Path.Split('.')
            : new[] { parameter.Name! };

        _requiredPaths.Add(path);

        return path;
    }

    private readonly List<string[]> _requiredPaths = new();
    public IReadOnlyList<string[]> Required => _requiredPaths;

    protected override bool ResolveDefaultIfNotExistsParameterValue(
        Type targetType,
        ParameterInfo parameter)
        => false;

}
