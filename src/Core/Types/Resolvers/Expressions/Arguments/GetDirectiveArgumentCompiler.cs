using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetDirectiveArgumentCompiler
       : ResolverParameterCompilerBase<IDirectiveContext>
    {
        private readonly PropertyInfo _directive;
        private readonly MethodInfo _argument;

        public GetDirectiveArgumentCompiler()
        {
            _directive = typeof(IDirectiveContext)
                .GetTypeInfo()
                .GetDeclaredProperty(nameof(IDirectiveContext.Directive));

            _argument = typeof(IDirective)
                .GetTypeInfo()
                .GetDeclaredMethod("GetArgument");
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            parameter.IsDefined(typeof(DirectiveArgumentAttribute));

        public override Expression Compile(
            ParameterInfo parameter,
            Type sourceType)
        {
            var attribute =
               parameter.GetCustomAttribute<DirectiveArgumentAttribute>();

            Expression getDirective = Expression.Property(Context, _directive);
            MethodInfo argument = _argument.MakeGenericMethod(
                parameter.ParameterType);

            return string.IsNullOrEmpty(attribute.Name)
                ? Expression.Call(getDirective, argument,
                    Expression.Constant(parameter.Name))
                : Expression.Call(getDirective, argument,
                    Expression.Constant(attribute.Name));
        }
    }
}
