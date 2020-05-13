using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
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
            bool errors = true;

            if (TryCoerceArguments())
            {
                await ExecuteResolverPipelineAsync().ConfigureAwait(false);
                errors = false;
            }

            if (Context.RequestAborted.IsCancellationRequested)
            {
                return;
            }

            CompleteValue(withErrors: errors);
        }

        private bool TryCoerceArguments()
        {
            if (_selection.Arguments.HasErrors)
            {
                foreach (PreparedArgument argument in _selection.Arguments.Values)
                {
                    if (argument.IsError)
                    {
                        Context.ReportError(argument.Error!.WithPath(Context.Path));
                    }
                }

                return false;
            }

            if (!_selection.IsFinal)
            {
                var args = new Dictionary<NameString, PreparedArgument>();

                foreach (PreparedArgument argument in _selection.Arguments.Values)
                {
                    if (argument.IsFinal)
                    {
                        args.Add(argument.Argument.Name, argument);
                    }
                    else
                    {
                        IValueNode literal = _operationContext.ReplaceVariables(
                            argument.ValueLiteral!, argument.Type);

                        args.Add(argument.Argument.Name, new PreparedArgument(
                            argument.Argument,
                            literal.TryGetValueKind(out ValueKind kind) ? kind : ValueKind.Unknown,
                            argument.IsFinal,
                            argument.IsImplicit,
                            null,
                            literal));
                    }
                }

                Context.Arguments = args;
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

        private void CompleteValue(bool withErrors)
        {
            try
            {
                Context.Result = withErrors
                    ? null
                    : ValueCompletion.Complete(
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
                _operationContext.Result.AddNonNullViolation(
                    Context.FieldSelection,
                    Context.Path,
                    Context.ResultMap);
            }
            else
            {
                Context.ResultMap.SetValue(
                    Context.ResponseIndex,
                    Context.ResponseName,
                    Context.Result,
                    Context.Field.Type.IsNullableType());
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
            _selection = selection;
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
            _selection = default!;
            Context.Clear();
        }
    }
}
