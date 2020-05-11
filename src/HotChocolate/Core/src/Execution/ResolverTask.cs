using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution.Utilities;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class ResolverTask
    {
        private ValueTask _task;
        private IOperationContext _operationContext = default!;
        private IPreparedSelection _selection = default!;

        public MiddlewareContext Context { get; } = new MiddlewareContext();

        public void BeginExecute() => _task = ExecuteAsync();

        public ValueTask<object?> EndExecuteAsync()
        {
            if (_task.IsCompletedSuccessfully)
            {
                return new ValueTask<object?>(Context.Result);
            }

            return AwaitHelper();
        }

        private async ValueTask<object?> AwaitHelper()
        {
            await _task;
            return Context.Result;
        }

        private async ValueTask ExecuteAsync()
        {
            if (TryCoerceArguments())
            {
                await ExecuteResolverPipelineAsync();
            }

            if (Context.RequestAborted.IsCancellationRequested)
            {
                return;
            }

            CompleteValue();
        }

        private bool TryCoerceArguments()
        {
            foreach(PreparedArgument argument in _selection.Arguments.Values)
            {
                if(argument.IsError) 
                {

                }
                
                // if(argument.)
            }

            return true;
        }

        private async ValueTask ExecuteResolverPipelineAsync()
        {
            try
            {
                await Context.ResolverPipeline(Context).ConfigureAwait(false);

                switch (Context.Result)
                {
                    case IError error:
                        Context.ReportError(error);
                        Context.Result = null;
                        break;

                    case IEnumerable<IError> errors:
                        foreach (IError error in errors)
                        {
                            Context.ReportError(error);
                        }
                        Context.Result = null;
                        break;
                }
            }
            catch (GraphQLException ex)
            {
                foreach (IError error in ex.Errors)
                {
                    Context.ReportError(error);
                }
                Context.Result = null;
            }
            catch (Exception ex)
            {
                Context.ReportError(ex);
                Context.Result = null;
            }
        }

        private void CompleteValue()
        {
            try
            {
                Context.Result = ValueCompletion.Complete(
                    _operationContext,
                    Context,
                    Context.Path,
                    Context.Field.Type,
                    Context.Result);
            }
            catch (GraphQLException ex)
            {
                foreach (IError error in ex.Errors)
                {
                    Context.ReportError(error);
                }
                Context.Result = null;
            }
            catch (Exception ex)
            {
                Context.ReportError(ex);
                Context.Result = null;
            }

            if (Context.Result is null && Context.Field.Type.IsNonNullType())
            {
                // _operationContext.NonNullViolations.Register(Context.FieldSelection, null);
            }
        }

        public void Initialize(
            IOperationContext operationContext,
            IPreparedSelection selection,
            ResultMap resultMap,
            int responseIndex,
            object? parent,
            Path path,
            IImmutableDictionary<string, object?> scopedContextData)
        {
            _task = default;
            _operationContext = operationContext;
            Context.Initialize(
                operationContext,
                selection,
                resultMap,
                responseIndex,
                parent,
                path,
                scopedContextData);
        }

        public void Clear()
        {
            _task = default;
            _operationContext = default!;
            Context.Clear();
        }
    }
}
