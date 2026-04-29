using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using GreenDonut;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Fetching;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DataLoaderParameterExpressionBuilder : CustomParameterExpressionBuilder
{
    private static readonly MethodInfo s_dataLoader;

    static DataLoaderParameterExpressionBuilder()
    {
        s_dataLoader = typeof(DataLoaderResolverContextExtensions)
            .GetMethods()
            .First(t => t.IsDefined(typeof(GetDataLoaderAttribute)));
    }

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(IDataLoader).IsAssignableFrom(parameter.ParameterType);

    [UnconditionalSuppressMessage(
        "AOT",
        "IL2060",
        Justification =
            "MakeGenericMethod is used with types known from resolver parameter metadata.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification =
            "MakeGenericMethod is used with types known from resolver parameter metadata.")]
    public override Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Call(
            s_dataLoader.MakeGenericMethod(context.Parameter.ParameterType),
            context.ResolverContext);
}
