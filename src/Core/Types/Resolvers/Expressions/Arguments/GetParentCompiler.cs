using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetParentCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly MethodInfo _parent;

        public GetParentCompiler()
        {
            _parent = ContextTypeInfo.GetDeclaredMethod(
                nameof(IResolverContext.Parent));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
        {
            return sourceType == parameter.ParameterType
                || parameter.IsDefined(typeof(ParentAttribute));
        }

        public override Expression Compile(
            ParameterInfo parameter,
            Type sourceType)
        {
            MethodInfo argumentMethod = _parent.MakeGenericMethod(
                parameter.ParameterType);

            return Expression.Call(Context, argumentMethod);
        }
    }
}
