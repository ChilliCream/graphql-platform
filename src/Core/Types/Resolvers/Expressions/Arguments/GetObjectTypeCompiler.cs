using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetObjectTypeCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly PropertyInfo _objectType;

        public GetObjectTypeCompiler()
        {
            _objectType = ContextTypeInfo.GetProperty(
                nameof(IResolverContext.ObjectType));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) => typeof(ObjectType) == parameter.ParameterType;

        public override Expression Compile(
            ParameterInfo parameter,
            Type sourceType)
        {
            return Expression.Property(Context, _objectType);
        }
    }
}
