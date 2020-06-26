using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal sealed partial class ResolverTask : ITask
    {
        private readonly MiddlewareContext _context = new MiddlewareContext();
        private ValueTask _task;
        private IOperationContext _operationContext = default!;
        private IPreparedSelection _selection = default!;

        public bool IsCompleted => _task.IsCompleted;

        public void BeginExecute()
        {
            _operationContext.Execution.TaskStats.TaskStarted();
            _task = ExecuteInternalAsync();
        }

        private async ValueTask ExecuteInternalAsync()
        {
            using (_operationContext.DiagnosticEvents.ResolveFieldValue(_context))
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
                    _operationContext.Execution.TaskPool.Return(this);
                }
            }
        }

        private bool TryCoerceArguments()
        {
            // if we have errors on the compiled execution plan we will report the errors and
            // signal that this resolver task has errors and shall end.
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

            try
            {
                // if there are arguments that have variables and need variable replacement we will
                // rewrite the arguments that need variable replacement.
                if (!_selection.Arguments.IsFinal)
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
                            IValueNode literal = VariableRewriter.Rewrite(
                                argument.ValueLiteral!, _context.Variables);

                            args.Add(argument.Argument.Name, new PreparedArgument(
                                argument.Argument,
                                literal.TryGetValueKind(out ValueKind kind) 
                                    ? kind 
                                    : ValueKind.Unknown,
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

            return false;
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
            object? completedValue = null;

            try
            {
                // we will only try to complete the resolver value if there are no known errors.
                if (!withErrors)
                {
                    if (ValueCompletion.TryComplete(
                        _operationContext,
                        _context,
                        _context.Path,
                        _context.Field.Type,
                        _context.Result,
                        out completedValue) &&
                        !_context.Field.Type.IsLeafType() &&
                        completedValue is IHasResultDataParent result)
                    {
                        result.Parent = _context.ResultMap;
                    }
                }
            }
            catch (Exception ex)
            {
                _context.ReportError(ex);
                _context.Result = null;
            }

            if (completedValue is null && _context.Field.Type.IsNonNullType())
            {
                // if we detect a non-null violation we will stash it for later.
                // the non-null propagation is delayed so that we can parallelize better.
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
                    completedValue,
                    _context.Field.Type.IsNullableType());
            }
        }
    }
}