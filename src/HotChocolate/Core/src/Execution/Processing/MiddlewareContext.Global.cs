using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Properties;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Processing
{
    internal partial class MiddlewareContext
    {
        private IOperationContext _operationContext = default!;
        private object? _resolverResult;
        private bool _hasResolverResult;

        public IServiceProvider Services => _operationContext.Services;

        public ISchema Schema => _operationContext.Schema;

        public IObjectType RootType => _operationContext.Operation.RootType;

        public DocumentNode Document => _operationContext.Operation.Document;

        public OperationDefinitionNode Operation => _operationContext.Operation.Definition;

        public IDictionary<string, object?> ContextData => _operationContext.ContextData;

        public IVariableValueCollection Variables => _operationContext.Variables;

        public CancellationToken RequestAborted => _operationContext.RequestAborted;

        public IReadOnlyList<IFieldSelection> GetSelections(
            ObjectType typeContext,
            SelectionSetNode? selectionSet = null,
            bool allowInternals = false)
        {
            if (typeContext is null)
            {
                throw new ArgumentNullException(nameof(typeContext));
            }

            selectionSet ??= _selection.SelectionSet;

            if (selectionSet is null)
            {
                return Array.Empty<IFieldSelection>();
            }

            ISelectionSet fields =
                _operationContext.CollectFields(selectionSet, typeContext);

            if (fields.IsConditional)
            {
                var finalFields = new List<IFieldSelection>();

                for (var i = 0; i < fields.Selections.Count; i++)
                {
                    ISelection selection = fields.Selections[i];
                    if (selection.IsIncluded(_operationContext.Variables, allowInternals))
                    {
                        finalFields.Add(selection);
                    }
                }

                return finalFields;
            }

            return fields.Selections;
        }

        public void ReportError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                throw new ArgumentException(
                    Resources.MiddlewareContext_ReportErrorCannotBeNull,
                    nameof(errorMessage));
            }

            ReportError(ErrorBuilder.New()
                .SetMessage(errorMessage)
                .SetPath(Path)
                .AddLocation(FieldSelection)
                .Build());
        }

        public void ReportError(Exception exception)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (exception is GraphQLException graphQLException)
            {
                foreach (IError error in graphQLException.Errors)
                {
                    ReportError(error);
                }
            }
            else
            {
                IError error = _operationContext.ErrorHandler
                    .CreateUnexpectedError(exception)
                    .SetPath(Path)
                    .AddLocation(FieldSelection)
                    .Build();

                ReportError(error);
            }
        }

        public void ReportError(IError error)
        {
            if (error is null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            error = _operationContext.ErrorHandler.Handle(error);
            _operationContext.Result.AddError(error, FieldSelection);
            _operationContext.DiagnosticEvents.ResolverError(this, error);
            HasErrors = true;
        }

        public async ValueTask<T> ResolveAsync<T>()
        {
            if (!_hasResolverResult)
            {
                _resolverResult = Field.Resolver is null
                    ? null
                    : await Field.Resolver(this).ConfigureAwait(false);
                _hasResolverResult = true;
            }

            return _resolverResult is null ? default! : (T)_resolverResult;
        }

        public T Resolver<T>() => _operationContext.Activator.GetOrCreate<T>();

        public T Service<T>()
        {
            return Services.GetRequiredService<T>();
        }

        public object Service(Type service)
        {
            if (service is null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            return Services.GetRequiredService(service);
        }
    }
}
