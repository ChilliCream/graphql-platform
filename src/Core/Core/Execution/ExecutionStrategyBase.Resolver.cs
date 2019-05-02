using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution
{
    internal abstract partial class ExecutionStrategyBase
    {
        protected static async Task ExecuteResolverAsync(
           ResolverContext resolverContext,
           IErrorHandler errorHandler)
        {
            Activity activity = resolverContext.BeginResolveField();

            try
            {
                await ExecuteMiddlewareAsync(resolverContext, errorHandler)
                    .ConfigureAwait(false);

                if (resolverContext.Result is IError singleError)
                {
                    resolverContext.ResolverError(singleError);
                }
                else if (resolverContext.Result is IEnumerable<IError> errors)
                {
                    resolverContext.ResolverError(errors);
                }
            }
            finally
            {
                resolverContext.EndResolveField(activity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task ExecuteMiddlewareAsync(
            ResolverContext resolverContext,
            IErrorHandler errorHandler)
        {
            try
            {
                await resolverContext.Middleware.Invoke(resolverContext)
                    .ConfigureAwait(false);
            }
            catch (QueryException ex)
            {
                resolverContext.Result = ex.Errors;
            }
            catch (Exception ex)
            {
                resolverContext.Result =
                    errorHandler.CreateUnexpectedError(ex)
                        .SetPath(resolverContext.Path)
                        .AddLocation(resolverContext.FieldSelection)
                        .Build();
            }
        }
    }
}
