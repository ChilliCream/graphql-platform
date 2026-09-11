using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers;

internal sealed class DirectiveParameterHandler : IParameterHandler
{
    private readonly Directive _directive;
    private readonly Type _runtimeType;

    public DirectiveParameterHandler(Directive directive)
    {
        _directive = directive;
        _runtimeType = directive.Type.RuntimeType;
    }

    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(Directive)
            || parameter.ParameterType == typeof(DirectiveNode)
            || (_runtimeType != typeof(object) && _runtimeType == parameter.ParameterType);

    public Expression CreateExpression(ParameterInfo parameter)
    {
        if (parameter.ParameterType == typeof(Directive))
        {
            return Expression.Constant(_directive, typeof(Directive));
        }

        if (parameter.ParameterType == typeof(DirectiveNode))
        {
            return Expression.Constant(_directive.ToSyntaxNode(), typeof(DirectiveNode));
        }

        return Expression.Constant(_directive.ToValue<object>(), _runtimeType);
    }
}
