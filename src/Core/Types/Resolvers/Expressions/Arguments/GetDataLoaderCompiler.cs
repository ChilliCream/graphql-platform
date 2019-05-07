using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetDataLoaderCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly MethodInfo _dataLoader;
        private readonly MethodInfo _dataLoaderWithKey;

        public GetDataLoaderCompiler()
        {
            _dataLoaderWithKey = typeof(DataLoaderResolverContextExtensions)
                .GetTypeInfo()
                .GetDeclaredMethod("DataLoader",
                    typeof(IResolverContext),
                    typeof(string));

            _dataLoader = typeof(DataLoaderResolverContextExtensions)
                .GetTypeInfo()
                .GetDeclaredMethod("DataLoader", typeof(IResolverContext));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            parameter.IsDefined(typeof(DataLoaderAttribute));

        public override Expression Compile(
            ParameterInfo parameter,
            Type sourceType)
        {
            var attribute =
                parameter.GetCustomAttribute<DataLoaderAttribute>();

            return string.IsNullOrEmpty(attribute.Key)
                ? Expression.Call(_dataLoader, Context)
                : Expression.Call(_dataLoaderWithKey, Context,
                    Expression.Constant(attribute.Key));
        }
    }
}
