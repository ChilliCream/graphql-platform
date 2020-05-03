using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution
{
    internal partial class MiddlewareContext : IMiddlewareContext
    {
        private readonly IOperationContext _operationContext;
        private Task<object?>? _resolverResult;

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
            try
            {
                return _operationContext.CollectFields(selectionSet, typeContext);
            }
            catch (GraphQLException ex)
            {
                throw new GraphQLException(ex.Errors.Select(error => error.WithPath(path)));
            }
        }

        [return: MaybeNull]
        public T CustomProperty<T>(string key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (ContextData.TryGetValue(key, out object? value))
            {
                if (value is null)
                {
                    return default;
                }

                if (value is T v)
                {
                    return v;
                }
            }

            // TODO : Copy resource
            throw new ArgumentException(
                "CoreResources.ResolverContext_CustomPropertyNotExists");
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

        public void ReportError(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            _operationContext.AddError(_operationContext.ErrorHandler.Handle(error));
        }

        [return: MaybeNull]
        public async Task<T> ResolveAsync<T>()
        {
            if (_resolverResult is null)
            {
                _resolverResult = Field.Resolver is null
                    ? Task.FromResult<object?>(null)
                    : Field.Resolver.Invoke(this);
            }

            object? result;

            if (_resolverResult.IsCompleted && _resolverResult.Exception is null)
            {
                result = _resolverResult.Result;
            }
            else
            {
                result = await _resolverResult.ConfigureAwait(false);
            }

            return result is null ? default! : (T)result;
        }

        public T Resolver<T>() => _operationContext.Activator.GetOrCreateResolver<T>();

        public T Service<T>()
        {
            return Services.GetRequiredService<T>();
        }

        public object Service(Type service)
        {
            return Services.GetRequiredService(service);
        }
    }
}
