using System;
using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetPathCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly PropertyInfo _path;

        public GetPathCompiler()
        {
            _path = ContextTypeInfo.GetProperty(nameof(IResolverContext.Path))!;
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            typeof(Path) == parameter.ParameterType;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType) =>
            Expression.Property(context, _path);
    }
}
