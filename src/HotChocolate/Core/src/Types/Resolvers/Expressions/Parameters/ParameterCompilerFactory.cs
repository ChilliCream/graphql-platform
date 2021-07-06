using System.Collections.Generic;

#nullable enable

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
            yield return new GetCustomContextCompiler();
#pragma warning restore CS0612

            yield return new GetCancellationTokenCompiler();
            yield return new GetContextCompiler<IResolverContext>();
            yield return new GetGlobalStateCompiler();
            yield return new SetGlobalStateCompiler();
            yield return new GetScopedStateCompiler();
            yield return new SetScopedStateCompiler();
            yield return new GetLocalStateCompiler();
            yield return new SetLocalStateCompiler();
            yield return new GetDataLoaderCompiler();
            yield return new GetEventMessageCompiler();
            yield return new GetFieldSelectionCompiler();
            yield return new GetFieldSyntaxCompiler();
            yield return new GetObjectTypeCompiler();
            yield return new GetOperationCompiler();
            yield return new GetOutputFieldCompiler();
            yield return new GetParentCompiler();
            yield return new GetQueryCompiler();
            yield return new GetSchemaCompiler();
            yield return new ScopedServiceCompiler();
            yield return new GetServiceCompiler();
            yield return new GetArgumentCompiler();
        }
    }
}
