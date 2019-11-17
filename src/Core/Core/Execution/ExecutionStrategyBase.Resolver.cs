using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal abstract partial class ExecutionStrategyBase
    {
        protected static async Task ExecuteResolverAsync(
           ResolverContext resolverContext,
           IErrorHandler errorHandler)
        {
            Activity activity = resolverContext.BeginResolveField();

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

            resolverContext.EndResolveField(activity);
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

                // TODO : this should be handled more elegant
                if (resolverContext.Result is IQueryable q)
                {
                    resolverContext.Result =
                        await Task.Run(() =>
                        { 
                            var items = new List<object>();
                            foreach (object o in q)
                            {
                                items.Add(o);
                            }
                            return items;
                        })
                        .ConfigureAwait(false);
                }
            }
            catch (GraphQLException ex)
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
