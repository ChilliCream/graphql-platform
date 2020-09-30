using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    public class MockResolverContext: IResolverContext
    {
        public IDictionary<string, object?> ContextData { get; }
        public IServiceProvider Services { get; }
        public ISchema Schema { get; }
        public IObjectType RootType { get; }
        public IObjectType ObjectType { get; }
        public IObjectField Field { get; }
        public DocumentNode Document { get; }
        public OperationDefinitionNode Operation { get; }
        public FieldNode FieldSelection { get; }
        public NameString ResponseName { get; }
        public Path Path { get; }
        public IImmutableDictionary<string, object?> ScopedContextData { get; set; }
        public IImmutableDictionary<string, object?> LocalContextData { get; set; }
        public IVariableValueCollection Variables { get; }
        public CancellationToken RequestAborted { get; }

        public MockResolverContext(ISchema schema)
        {
            Schema = schema;
        }

        public T Parent<T>()
        {
            throw new NotImplementedException();
        }

        public T Argument<T>(NameString name)
        {
            throw new NotImplementedException();
        }

        public T ArgumentValue<T>(NameString name)
        {
            throw new NotImplementedException();
        }

        public TValueNode ArgumentLiteral<TValueNode>(NameString name) where TValueNode : IValueNode
        {
            throw new NotImplementedException();
        }

        public Optional<T> ArgumentOptional<T>(NameString name)
        {
            throw new NotImplementedException();
        }

        public ValueKind ArgumentKind(NameString name)
        {
            throw new NotImplementedException();
        }

        public T Service<T>()
        {
            throw new NotImplementedException();
        }

        public object Service(Type service)
        {
            throw new NotImplementedException();
        }

        public T Resolver<T>()
        {
            throw new NotImplementedException();
        }

        public void ReportError(string errorMessage)
        {
            throw new NotImplementedException();
        }

        public void ReportError(IError error)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<IFieldSelection> GetSelections(
            ObjectType typeContext,
            SelectionSetNode? selectionSet = null,
            bool allowInternals = false)
        {
            throw new NotImplementedException();
        }
    }
}
