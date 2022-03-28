using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Data.Filters;

internal sealed class FilterContextParameterExpressionBuilder
    : IParameterExpressionBuilder
{
    private const string _getFilterContext =
        nameof(FilterContextResolverContextExtensions.GetFilterContext);

    private static readonly MethodInfo _getFilterContextMethod =
        typeof(FilterContextResolverContextExtensions)
            .GetMethods(BindingFlags.Static)
            .First(method => method.Name.Equals(_getFilterContext, StringComparison.Ordinal));

    public ArgumentKind Kind => ArgumentKind.Service;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(IFilterContext);

    public Expression Build(ParameterInfo parameter, Expression context)
        => Expression.Call(_getFilterContextMethod, context);
}
