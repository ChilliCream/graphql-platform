using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetArgumentCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly MethodInfo _argument;

        public GetArgumentCompiler()
        {
            _argument = ContextTypeInfo.GetDeclaredMethod(
                nameof(IResolverContext.Argument));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) => true;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            string name = parameter.IsDefined(typeof(GraphQLNameAttribute))
                ? parameter.GetCustomAttribute<GraphQLNameAttribute>().Name
                : parameter.Name;

            MethodInfo argumentMethod = _argument.MakeGenericMethod(
                parameter.ParameterType);

            return Expression.Call(context, argumentMethod,
                Expression.Constant(new NameString(name)));
        }
    }
}
