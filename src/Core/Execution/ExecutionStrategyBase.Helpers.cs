using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal abstract partial class ExecutionStrategyBase
    {
        private static readonly FieldValueCompleter _fieldValueCompleter =
            new FieldValueCompleter();

        protected static object ExecuteResolver(
           ResolverTask resolverTask,
           bool isDeveloperMode,
           CancellationToken cancellationToken)
        {
            if (resolverTask.HasExecutableDirectives)
            {
                return ExecuteDirectiveResolversAsync(
                    resolverTask, isDeveloperMode,
                    cancellationToken);
            }
            else
            {
                return ExecuteFieldResolver(
                    resolverTask, isDeveloperMode,
                    cancellationToken);
            }
        }

        private static object ExecuteFieldResolver(
            ResolverTask resolverTask,
            bool isDeveloperMode,
            CancellationToken cancellationToken)
        {
            try
            {
                if (resolverTask.FieldSelection.Field.Resolver == null)
                {
                    return null;
                }

                return resolverTask.FieldSelection.Field.Resolver(
                    resolverTask.ResolverContext,
                    cancellationToken);
            }
            catch (QueryException ex)
            {
                return ex.Errors;
            }
            catch (Exception ex)
            {
                return CreateErrorFromException(ex,
                    resolverTask.FieldSelection.Selection,
                    isDeveloperMode);
            }
        }

        private static Func<Task<object>> CreateFieldResolverDelegate(
            ResolverTask resolverTask,
            bool isDeveloperMode,
            CancellationToken cancellationToken)
        {
            return new Func<Task<object>>(async () =>
            {
                object resolverResult = ExecuteFieldResolver(
                    resolverTask, isDeveloperMode, cancellationToken);

                return await FinalizeResolverResultAsync(
                    resolverTask.FieldSelection.Selection,
                    resolverResult,
                    isDeveloperMode);
            });
        }

        private static async Task<object> ExecuteDirectiveResolversAsync(
            ResolverTask resolverTask,
            bool isDeveloperMode,
            CancellationToken cancellationToken)
        {
            try
            {
                return await OnInvokeResolverAsync(
                    resolverTask,
                    isDeveloperMode,
                    cancellationToken);
            }
            catch (QueryException ex)
            {
                return ex.Errors;
            }
            catch (Exception ex)
            {
                return CreateErrorFromException(ex,
                    resolverTask.FieldSelection.Selection,
                    isDeveloperMode);
            }
        }

        public DirectiveDelegate Build()
        {
            DirectiveDelegate last = context =>
            {
                return Task.CompletedTask;
            };

            foreach (Middleware component in _components.Reverse())
            {
                last = component(last);
            }

            return last;
        }

        private static Task<object> OnInvokeResolverAsync(
            ResolverTask resolverTask,
            bool isDeveloperMode,
            CancellationToken cancellationToken)
        {

            List<Middleware> middlewares;

            foreach (Middleware m in middlewares)
            {
                m.Invoke()
            }




            Func<Task<object>> current = CreateFieldResolverDelegate(
                resolverTask, isDeveloperMode, cancellationToken);




            foreach (IDirective directive in resolverTask.ExecutableDirectives)
            {
                if (directive.OnInvokeResolver != null)
                {
                    current = CreateDirectiveResolverDelegate(
                        directive, resolverTask, current,
                        isDeveloperMode, cancellationToken);
                }
            }

            return current();
        }

        private static async Task OnAfterInvokeResolverAsync(
            ResolverTask resolverTask,
            bool isDeveloperMode,
            CancellationToken cancellationToken)
        {
            foreach (IDirective directive in resolverTask.ExecutableDirectives)
            {
                if (directive.OnAfterInvokeResolver != null)
                {
                    resolverTask.ResolverResult =
                        await directive.OnAfterInvokeResolver(
                            resolverTask.ResolverContext,
                            directive,
                            resolverTask.ResolverResult,
                            cancellationToken);
                }
            }
        }

        private static async Task<object> ExecuteDirectiveResolverAsync(
            IDirective directive,
            ResolverTask resolverTask,
            Func<Task<object>> resolvePrevious,
            CancellationToken cancellationToken)
        {
            return await directive.OnInvokeResolver(
                resolverTask.ResolverContext,
                directive,
                resolvePrevious,
                cancellationToken);
        }

        private static Func<Task<object>> CreateDirectiveResolverDelegate(
            IDirective directive,
            ResolverTask resolverTask,
            Func<Task<object>> resolvePrevious,
            bool isDeveloperMode,
            CancellationToken cancellationToken)
        {
            return new Func<Task<object>>(async () =>
            {
                object resolverResult = await ExecuteDirectiveResolverAsync(
                    directive, resolverTask, resolvePrevious,
                    cancellationToken);

                return await FinalizeResolverResultAsync(
                    resolverTask.FieldSelection.Selection,
                    resolverResult,
                    isDeveloperMode);
            });
        }

        protected static Task<object> FinalizeResolverResultAsync(
            FieldNode fieldSelection,
            object resolverResult,
            bool isDeveloperMode)
        {
            switch (resolverResult)
            {
                case Task<object> task:
                    return FinalizeResolverResultTaskAsync(
                        fieldSelection, task, isDeveloperMode);

                case IResolverResult result:
                    return Task.FromResult(
                        CompleteResolverResult(fieldSelection, result));

                default:
                    return Task.FromResult(resolverResult);
            }
        }

        private static async Task<object> FinalizeResolverResultTaskAsync(
            FieldNode fieldSelection,
            Task<object> task,
            bool isDeveloperMode)
        {
            try
            {
                object resolverResult = task.IsCompleted
                    ? task.Result
                    : await task;

                if (resolverResult is IResolverResult r)
                {
                    return CompleteResolverResult(fieldSelection, r);
                }
                return resolverResult;
            }
            catch (QueryException ex)
            {
                return ex.Errors;
            }
            catch (Exception ex)
            {
                return CreateErrorFromException(ex,
                    fieldSelection, isDeveloperMode);
            }
        }

        private static IQueryError CreateErrorFromException(
            Exception exception,
            FieldNode fieldSelection,
            bool isDeveloperMode)
        {
            if (isDeveloperMode)
            {
                return new FieldError(
                    $"{exception.Message}\r\n\r\n{exception.StackTrace}",
                    fieldSelection);
            }
            else
            {
                return new FieldError(
                    "Unexpected execution error.",
                    fieldSelection);
            }
        }

        protected void CompleteValue(
            FieldValueCompletionContext completionContext)
        {
            _fieldValueCompleter.CompleteValue(completionContext);
        }

        private static object CompleteResolverResult(
            FieldNode fieldSelection,
            IResolverResult resolverResult)
        {
            if (resolverResult.IsError)
            {
                return new FieldError(
                    resolverResult.ErrorMessage,
                    fieldSelection);
            }
            return resolverResult.Value;
        }
    }
}
