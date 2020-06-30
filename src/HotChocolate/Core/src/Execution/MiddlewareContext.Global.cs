using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Execution.Utilities;

namespace HotChocolate.Execution
{
    internal partial class MiddlewareContext : IMiddlewareContext
    {
        private IOperationContext _operationContext = default!;
        private object? _resolverResult;
        private bool _hasResolverResult;

        public IServiceProvider Services => _operationContext.Services;

        public ISchema Schema => _operationContext.Schema;

        public ObjectType RootType => _operationContext.Operation.RootType;

        public DocumentNode Document => _operationContext.Operation.Document;

        public OperationDefinitionNode Operation => _operationContext.Operation.Definition;

        public IDictionary<string, object?> ContextData => _operationContext.ContextData;

        public IVariableValueCollection Variables => _operationContext.Variables;

        public CancellationToken RequestAborted => _operationContext.RequestAborted;

        public IReadOnlyList<IFieldSelection> CollectFields(
            ObjectType typeContext)
        {
            if (FieldSelection.SelectionSet is null)
            {
                return Array.Empty<IFieldSelection>();
            }

            return CollectFields(typeContext, FieldSelection.SelectionSet, Path);
        }

        public IReadOnlyList<IFieldSelection> CollectFields(
            ObjectType typeContext, SelectionSetNode selectionSet)
        {
            return CollectFields(typeContext, selectionSet, Path);
        }

        public IReadOnlyList<IFieldSelection> CollectFields(
            ObjectType typeContext, SelectionSetNode selectionSet, Path path)
        {
            if (typeContext is null)
            {
                throw new ArgumentNullException(nameof(typeContext));
            }

            if (selectionSet is null)
            {
                throw new ArgumentNullException(nameof(selectionSet));
            }

            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            try
            {
                IPreparedSelectionList fields = _operationContext.CollectFields(
                    selectionSet, typeContext);

                if (fields.IsFinal)
                {
                    return fields;
                }

                var finalFields = new List<IFieldSelection>();

                for (var i = 0; i < fields.Count; i++)
                {
                    IPreparedSelection selection = fields[i];
                    if (selection.IsFinal || selection.IsVisible(_operationContext.Variables))
                    {
                        finalFields.Add(selection);
                    }
                }

                return finalFields;
            }
            catch (GraphQLException ex)
            {
                throw new GraphQLException(ex.Errors.Select(error => error.WithPath(path)));
            }
        }

        public void ReportError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                throw new ArgumentException(
                    "errorMessage mustn't be null or empty.",
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

            IError error = _operationContext.ErrorHandler
                .CreateUnexpectedError(exception)
                .SetPath(Path)
                .AddLocation(FieldSelection)
                .Build();

            ReportError(error);
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
                    : await Field.Resolver(this);
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
