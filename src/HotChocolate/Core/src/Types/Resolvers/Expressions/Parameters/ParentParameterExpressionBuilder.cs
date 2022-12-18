using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

/// <summary>
/// Builds parameter expressions injecting the parent object.
/// Parameters representing the parent object must be annotated with
/// <see cref="ParentAttribute"/>.
/// </summary>
internal sealed class ParentParameterExpressionBuilder : IParameterExpressionBuilder
{
    private const string _parent = nameof(IPureResolverContext.Parent);
    private static readonly MethodInfo _getParentMethod =
        _getParentMethod = PureContextType.GetMethods().First(IsParentMethod);

    private static bool IsParentMethod(MethodInfo method)
        => method.Name.Equals(_parent, StringComparison.Ordinal) &&
           method.IsGenericMethod;

    public ArgumentKind Kind => ArgumentKind.Source;

    public bool IsPure => true;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(ParentAttribute));

    public Expression Build(ParameterInfo parameter, Expression context)
    {
        var parameterType = parameter.ParameterType;
        var argumentMethod = _getParentMethod.MakeGenericMethod(parameterType);
        return Expression.Call(context, argumentMethod);
    }
}
