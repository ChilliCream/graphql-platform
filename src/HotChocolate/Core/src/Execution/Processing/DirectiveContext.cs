using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    internal sealed class DirectiveContext : IDirectiveContext
    {
        private readonly IMiddlewareContext _middlewareContext;

        public DirectiveContext(IMiddlewareContext middlewareContext, IDirective directive)
        {
            _middlewareContext = middlewareContext;
            Directive = directive;
        }

        public IDirective Directive { get; }

        public object? Result
        {
            get => _middlewareContext.Result;
            set => _middlewareContext.Result = value;
        }

        public bool IsResultModified =>
            _middlewareContext.IsResultModified;

        public IServiceProvider Services => _middlewareContext.Services;

        public ISchema Schema =>
            _middlewareContext.Schema;

        public IObjectType RootType =>
            _middlewareContext.RootType;

        public IObjectType ObjectType =>
            _middlewareContext.ObjectType;

        public IObjectField Field =>
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

        public IDictionary<string, object?> ContextData =>
            _middlewareContext.ContextData;

        public IImmutableDictionary<string, object?> ScopedContextData
        {
            get => _middlewareContext.ScopedContextData;
            set => _middlewareContext.ScopedContextData = value;
        }

        public NameString ResponseName =>
            _middlewareContext.ResponseName;

        public IVariableValueCollection Variables =>
            _middlewareContext.Variables;

        public IImmutableDictionary<string, object?> LocalContextData
        {
            get => _middlewareContext.LocalContextData;
            set => _middlewareContext.LocalContextData = value;
        }
        public IType? ValueType
        {
            get => _middlewareContext.ValueType;
            set => _middlewareContext.ValueType = value;
        }

        [Obsolete(
            "Use ArgumentValue<T>(name) or " +
            "ArgumentLiteral<TValueNode>(name) or " +
            "ArgumentOptional<T>(name).")]
        [return: MaybeNull]
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

        public ValueTask<T> ResolveAsync<T>() =>
            _middlewareContext.ResolveAsync<T>();

        public ValueKind ArgumentKind(NameString name) =>
            _middlewareContext.ArgumentKind(name);

        public T ArgumentValue<T>(NameString name) =>
            _middlewareContext.ArgumentValue<T>(name);

        public T ArgumentLiteral<T>(NameString name) where T : IValueNode =>
            _middlewareContext.ArgumentLiteral<T>(name);

        public Optional<T> ArgumentOptional<T>(NameString name) =>
            _middlewareContext.ArgumentOptional<T>(name);

        public IReadOnlyList<IFieldSelection> GetSelections(
            ObjectType typeContext,
            SelectionSetNode? selectionSet = null,
            bool allowInternals = false) =>
            _middlewareContext.GetSelections(typeContext, selectionSet, allowInternals);
    }
}
