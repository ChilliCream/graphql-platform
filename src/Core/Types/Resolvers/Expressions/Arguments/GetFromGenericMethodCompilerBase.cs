using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal abstract class GetFromGenericMethodCompilerBase<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        protected MethodInfo GenericMethod { get; set; }

        public override Expression Compile(
            ParameterInfo parameter,
            Type sourceType)
        {
            MethodInfo argumentMethod = GenericMethod.MakeGenericMethod(
                parameter.ParameterType);

            return Expression.Call(Context, argumentMethod);
        }
    }
}
