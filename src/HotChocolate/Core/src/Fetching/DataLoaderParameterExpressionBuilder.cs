using System.Linq.Expressions;
using System.Reflection;
using GreenDonut;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Fetching;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DataLoaderParameterExpressionBuilder : CustomParameterExpressionBuilder
{
    private static readonly MethodInfo _dataLoader;

    static DataLoaderParameterExpressionBuilder()
    {
        _dataLoader = typeof(DataLoaderResolverContextExtensions)
            .GetMethods()
            .First(t => t.IsDefined(typeof(GetDataLoaderAttribute)));
    }

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(IDataLoader).IsAssignableFrom(parameter.ParameterType);

    public override Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Call(
            _dataLoader.MakeGenericMethod(context.Parameter.ParameterType),
            context.ResolverContext);
}
