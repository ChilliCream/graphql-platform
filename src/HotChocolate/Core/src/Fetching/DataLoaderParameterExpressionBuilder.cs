using System.Linq;
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
    private static readonly MethodInfo _dataLoaderWithKey;

    static DataLoaderParameterExpressionBuilder()
    {
        _dataLoaderWithKey = typeof(DataLoaderResolverContextExtensions)
            .GetMethods()
            .First(t => t.IsDefined(typeof(GetDataLoaderWithKeyAttribute)));

        _dataLoader = typeof(DataLoaderResolverContextExtensions)
            .GetMethods()
            .First(t => t.IsDefined(typeof(GetDataLoaderAttribute)));
    }

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(DataLoaderAttribute)) ||
           typeof(IDataLoader).IsAssignableFrom(parameter.ParameterType);

    public override Expression Build(ParameterInfo parameter, Expression context)
    {
        var attribute = parameter.GetCustomAttribute<DataLoaderAttribute>();

        return string.IsNullOrEmpty(attribute?.Key)
            ? Expression.Call(
                _dataLoader.MakeGenericMethod(parameter.ParameterType),
                context)
            : Expression.Call(
                _dataLoaderWithKey.MakeGenericMethod(parameter.ParameterType),
                context,
                Expression.Constant(attribute.Key));
    }
}
