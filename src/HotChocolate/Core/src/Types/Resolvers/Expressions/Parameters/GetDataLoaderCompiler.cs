using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

#nullable enable

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
                .GetDeclaredMethod(
                    nameof(DataLoaderResolverContextExtensions.DataLoader),
                    typeof(IResolverContext),
                    typeof(string));

            _dataLoader = typeof(DataLoaderResolverContextExtensions)
                .GetTypeInfo()
                .GetDeclaredMethod(
                    nameof(DataLoaderResolverContextExtensions.DataLoader),
                    typeof(IResolverContext));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            ArgumentHelper.IsDataLoader(parameter);

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            var attribute = parameter.GetCustomAttribute<DataLoaderAttribute>();

            return string.IsNullOrEmpty(attribute?.Key)
                ? Expression.Call(
                    _dataLoader.MakeGenericMethod(parameter.ParameterType),
                    context)
                : Expression.Call(
                    _dataLoaderWithKey.MakeGenericMethod(parameter.ParameterType),
                    context,
                    Expression.Constant(attribute.Key));
        }
    }
}
