using System.Collections.Generic;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal static class ParameterCompilerFactory
    {
        public static IEnumerable<IResolverParameterCompiler> CreateForResolverContext()
        {
            return CreateFor<IResolverContext>();
        }

        public static IEnumerable<IResolverParameterCompiler> CreateForDirectiveContext()
        {
            yield return new GetDirectiveCompiler();
            yield return new GetDirectiveArgumentCompiler();
            yield return new GetContextCompiler<IDirectiveContext, IDirectiveContext>();

            foreach (IResolverParameterCompiler compiler in
                CreateFor<IDirectiveContext>())
            {
                yield return compiler;
            }
        }

        private static IEnumerable<IResolverParameterCompiler> CreateFor<T>()
            where T : IResolverContext
        {
            yield return new GetCancellationTokenCompiler<T>();
            yield return new GetContextCompiler<T, IResolverContext>();
            yield return new GetDataLoaderCompiler<T>();
            yield return new GetEventMessageCompiler<T>();
            yield return new GetFieldSelectionCompiler<T>();
            yield return new GetObjectTypeCompiler<T>();
            yield return new GetOperationCompiler<T>();
            yield return new GetOutputFieldCompiler<T>();
            yield return new GetParentCompiler<T>();
            yield return new GetQueryCompiler<T>();
            yield return new GetSchemaCompiler<T>();
            yield return new GetServiceCompiler<T>();
            yield return new GetArgumentCompiler<T>();
        }
    }
}
