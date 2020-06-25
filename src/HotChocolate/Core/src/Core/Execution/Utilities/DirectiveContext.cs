using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class DirectiveContext
        : IDirectiveContext
    {
        private readonly IMiddlewareContext _middlewareContext;

        public DirectiveContext(
            IMiddlewareContext middlewareContext,
            IDirective directive)
        {
            _middlewareContext = middlewareContext
                ?? throw new ArgumentNullException(nameof(middlewareContext));
            Directive = directive
                ?? throw new ArgumentNullException(nameof(directive));
        }

        public IDirective Directive { get; }

        public object Result
        {
            get => _middlewareContext.Result;
            set => _middlewareContext.Result = value;
        }

        public bool IsResultModified =>
            _middlewareContext.IsResultModified;

        public ISchema Schema =>
            _middlewareContext.Schema;

        public ObjectType RootType =>
        _middlewareContext.RootType;

        public ObjectType ObjectType =>
            _middlewareContext.ObjectType;

        public ObjectField Field =>
            _middlewareContext.Field;

        public DocumentNode Document => _middlewareContext.Document;

        public DocumentNode QueryDocument => Document;

        public OperationDefinitionNode Operation =>
            _middlewareContext.Operation;

        public FieldNode FieldSelection =>
            _middlewareContext.FieldSelection;

        public Path Path =>
            _middlewareContext.Path;

        public CancellationToken CancellationToken =>
            RequestAborted;

        public CancellationToken RequestAborted =>
            _middlewareContext.RequestAborted;

        public IDictionary<string, object> ContextData =>
            _middlewareContext.ContextData;

        public IImmutableDictionary<string, object> ScopedContextData
        {
            get => _middlewareContext.ScopedContextData;
            set => _middlewareContext.ScopedContextData = value;
        }

        public NameString ResponseName =>
            _middlewareContext.ResponseName;

        public IVariableValueCollection Variables =>
            _middlewareContext.Variables;

        public IImmutableDictionary<string, object> LocalContextData
        {
            get => _middlewareContext.LocalContextData;
            set => _middlewareContext.LocalContextData = value;
        }

        public IServiceProvider Services => throw new NotImplementedException();

        public IType ValueType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public T Argument<T>(NameString name) =>
            _middlewareContext.Argument<T>(name);

        public T Parent<T>() => _middlewareContext.Parent<T>();

        public void ReportError(string errorMessage) =>
            _middlewareContext.ReportError(errorMessage);

        public void ReportError(IError error) =>
            _middlewareContext.ReportError(error);

        public T Resolver<T>() =>
            _middlewareContext.Resolver<T>();

        public T Service<T>() =>
            _middlewareContext.Service<T>();

        public object Service(Type service) =>
            _middlewareContext.Service(service);

        public IReadOnlyList<IFieldSelection> CollectFields(
            ObjectType typeContext) =>
            _middlewareContext.CollectFields(typeContext);

        public IReadOnlyList<IFieldSelection> CollectFields(
            ObjectType typeContext, SelectionSetNode selectionSet) =>
            _middlewareContext.CollectFields(typeContext, selectionSet);

        public IReadOnlyList<IFieldSelection> CollectFields(
            ObjectType typeContext, SelectionSetNode selectionSet, Path path) =>
            _middlewareContext.CollectFields(typeContext, selectionSet, path);

        public ValueKind ArgumentKind(NameString name) =>
            _middlewareContext.ArgumentKind(name);

        public T ArgumentValue<T>(NameString name)
        {
            throw new NotImplementedException();
        }

        public T ArgumentLiteral<T>(NameString name) where T : IValueNode
        {
            throw new NotImplementedException();
        }

        public Optional<T> ArgumentOptional<T>(NameString name)
        {
            throw new NotImplementedException();
        }

        ValueTask<T> IMiddlewareContext.ResolveAsync<T>()
        {
            throw new NotImplementedException();
        }
    }
}
