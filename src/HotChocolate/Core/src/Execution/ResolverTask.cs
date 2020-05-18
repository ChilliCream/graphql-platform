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
        private readonly MiddlewareContext _context = new MiddlewareContext();
        private ValueTask _task;
        private IOperationContext _operationContext = default!;
        private IPreparedSelection _selection = default!;

        public bool IsCompleted => _task.IsCompleted;

        public void BeginExecute()
        {
            _operationContext.Execution.TaskStats.TaskStarted();
            _task = ExecuteAsync();
        }

        public async ValueTask EndExecuteAsync()
        {
            try
            {
                await _task.ConfigureAwait(false);
            }
            catch
            {
                // ignore any issues here
            }
        }

        private async ValueTask ExecuteAsync()
        {
            try
            {
                bool errors = true;

                if (TryCoerceArguments())
                {
                    await ExecuteResolverPipelineAsync().ConfigureAwait(false);
                    errors = false;
                }

                if (_context.RequestAborted.IsCancellationRequested)
                {
                    return;
                }

                CompleteValue(withErrors: errors);
            }
            finally
            {
                _operationContext.Execution.TaskStats.TaskCompleted();
            }
        }

        private bool TryCoerceArguments()
        {
            if (_selection.Arguments.HasErrors)
            {
                foreach (PreparedArgument argument in _selection.Arguments.Values)
                {
                    if (argument.IsError)
                    {
                        _context.ReportError(argument.Error!.WithPath(_context.Path));
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

                _context.Arguments = args;
            }

            return true;
        }

        private async ValueTask ExecuteResolverPipelineAsync()
        {
            try
            {
                await _context.ResolverPipeline(_context).ConfigureAwait(false);

                switch (_context.Result)
                {
                    case IError error:
                        _context.ReportError(error);
                        _context.Result = null;
                        break;

                    case IEnumerable<IError> errors:
                        foreach (IError error in errors)
                        {
                            _context.ReportError(error);
                        }
                        _context.Result = null;
                        break;
                }
            }
            catch (GraphQLException ex)
            {
                foreach (IError error in ex.Errors)
                {
                    _context.ReportError(error);
                }
                _context.Result = null;
            }
            catch (Exception ex)
            {
                _context.ReportError(ex);
                _context.Result = null;
            }
        }

        private void CompleteValue(bool withErrors)
        {
            try
            {
                _context.Result = withErrors
                    ? null
                    : ValueCompletion.Complete(
                        _operationContext,
                        _context,
                        _context.Path,
                        _context.Field.Type,
                        _context.Result);
            }
            catch (GraphQLException ex)
            {
                foreach (IError error in ex.Errors)
                {
                    _context.ReportError(error);
                }
                _context.Result = null;
            }
            catch (Exception ex)
            {
                _context.ReportError(ex);
                _context.Result = null;
            }

            if (_context.Result is null && _context.Field.Type.IsNonNullType())
            {
                _operationContext.Result.AddNonNullViolation(
                    _context.FieldSelection,
                    _context.Path,
                    _context.ResultMap);
            }
            else
            {
                _context.ResultMap.SetValue(
                    _context.ResponseIndex,
                    _context.ResponseName,
                    _context.Result,
                    _context.Field.Type.IsNullableType());
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
            _context.Initialize(
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
            _context.Clear();
        }
    }
}
