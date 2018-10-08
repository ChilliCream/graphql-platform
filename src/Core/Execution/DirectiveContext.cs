using System;
using System.Collections.Immutable;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class DirectiveContext
        : IDirectiveContext
    {
        private readonly IResolverContext _resolverContext;

        public DirectiveContext(IResolverContext resolverContext)
        {
            _resolverContext = resolverContext
                ?? throw new ArgumentNullException(nameof(resolverContext));
        }

        public IDirective Directive { get; set; }

        public object Result { get; set; }

        public ISchema Schema => _resolverContext.Schema;

        public ObjectType ObjectType => _resolverContext.ObjectType;

        public ObjectField Field => _resolverContext.Field;

        public DocumentNode QueryDocument => _resolverContext.QueryDocument;

        public OperationDefinitionNode Operation => _resolverContext.Operation;

        public FieldNode FieldSelection => _resolverContext.FieldSelection;

        public ImmutableStack<object> Source => _resolverContext.Source;

        public Path Path => _resolverContext.Path;

        public CancellationToken CancellationToken =>
            _resolverContext.CancellationToken;

        public T Argument<T>(string name) =>
            _resolverContext.Argument<T>(name);

        public T CustomContext<T>() => _resolverContext.CustomContext<T>();

        public T DataLoader<T>(string key) =>
            _resolverContext.DataLoader<T>(key);

        public T Parent<T>() => _resolverContext.Parent<T>();

        public void ReportError(string errorMessage) =>
            _resolverContext.ReportError(errorMessage);

        public T Resolver<T>() => _resolverContext.Resolver<T>();

        public T Service<T>() => _resolverContext.Service<T>();
    }
}
