using System.Collections.Generic;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal static class ParameterCompilerFactory
    {
        public static IEnumerable<IResolverParameterCompiler> Create()
        {
            return CreateFor<IResolverContext>();
        }

        private static IEnumerable<IResolverParameterCompiler> CreateFor<T>()
            where T : IResolverContext
        {
#pragma warning disable CS0612
            // this compile is obsolete
            yield return new GetCustomContextCompiler<T>();
#pragma warning restore CS0612

            yield return new GetCancellationTokenCompiler<T>();
            yield return new GetContextCompiler<T, IResolverContext>();
            yield return new GetGlobalStateCompiler<T>();
            yield return new SetGlobalStateCompiler<T>();
            yield return new GetScopedStateCompiler<T>();
            yield return new SetScopedStateCompiler<T>();
            yield return new GetLocalStateCompiler<T>();
            yield return new SetLocalStateCompiler<T>();
            yield return new GetDataLoaderCompiler<T>();
            yield return new GetEventMessageCompiler<T>();
            yield return new GetFieldSelectionCompiler<T>();
            yield return new GetObjectTypeCompiler<T>();
            yield return new GetOperationCompiler<T>();
            yield return new GetOutputFieldCompiler<T>();
            yield return new GetParentCompiler<T>();
            yield return new GetQueryCompiler<T>();
            yield return new GetSchemaCompiler<T>();
            yield return new ScopedServiceCompiler<T>();
            yield return new GetServiceCompiler<T>();
            yield return new GetArgumentCompiler<T>();
        }
    }
}
