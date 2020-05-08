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
            // todo: coerce arguments

            await Context.ResolverPipeline(Context).ConfigureAwait(false);

            if (Context.Result is IError error)
            {
                _operationContext.AddError(error, Context.FieldSelection);
                Context.Result = null;
            }
            else if (Context.Result is IEnumerable<IError> errors)
            {
                _operationContext.AddErrors(errors, Context.FieldSelection);
                Context.Result = null;
            }

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
                _operationContext.AddError(null, Context.FieldSelection);
                Context.Result = null;
            }
            catch (Exception ex)
            {
                _operationContext.AddError(null, Context.FieldSelection);
                Context.Result = null;
            }

            if (Context.Result is null && Context.Field.Type.IsNonNullType())
            {
                _operationContext.NonNullViolations.Register(Context.FieldSelection, null);
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
