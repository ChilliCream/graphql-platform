using System;
using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal abstract class GetFromGenericMethodCompilerBase : ResolverParameterCompilerBase
    {
        protected MethodInfo GenericMethod { get; set; } = default!;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            MethodInfo argumentMethod = GenericMethod.MakeGenericMethod(parameter.ParameterType);
            return Expression.Call(context, argumentMethod);
        }
    }
}
