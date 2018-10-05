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
        public DirectiveContext(IResolverContext context)
        {

        }

        public IDirective Directive { get; set; }

        public object Result { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ISchema Schema => throw new NotImplementedException();

        public ObjectType ObjectType => throw new NotImplementedException();

        public ObjectField Field => throw new NotImplementedException();

        public DocumentNode QueryDocument => throw new NotImplementedException();

        public OperationDefinitionNode Operation => throw new NotImplementedException();

        public FieldNode FieldSelection => throw new NotImplementedException();

        public ImmutableStack<object> Source => throw new NotImplementedException();

        public Path Path => throw new NotImplementedException();

        public CancellationToken CancellationToken => throw new NotImplementedException();

        public T Argument<T>(string name)
        {
            throw new NotImplementedException();
        }

        public T CustomContext<T>()
        {
            throw new NotImplementedException();
        }

        public T DataLoader<T>(string key)
        {
            throw new NotImplementedException();
        }

        public T Parent<T>()
        {
            throw new NotImplementedException();
        }

        public void ReportError(string errorMessage)
        {
            throw new NotImplementedException();
        }

        public T Resolver<T>()
        {
            throw new NotImplementedException();
        }

        public T Service<T>()
        {
            throw new NotImplementedException();
        }
    }
}
