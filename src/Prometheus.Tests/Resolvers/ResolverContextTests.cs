using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQLParser;
using Newtonsoft.Json;
using Xunit;
using Prometheus.Execution;
using Prometheus.Resolvers;
using Moq;
using System;
using Prometheus.Abstractions;
using System.Linq;

namespace Prometheus.Resolvers
{
    public class ResolverContextTests
    {
        [Fact]
        public void CreateRootContext()
        {
            // arrange
            Mock<IServiceProvider> services = new Mock<IServiceProvider>(MockBehavior.Strict);
            Mock<ISchema> schema = new Mock<ISchema>();
            Mock<IVariableCollection> variables = new Mock<IVariableCollection>(MockBehavior.Strict);
            OperationContext operationContext = new OperationContext(
                schema.Object, new QueryDocument(Enumerable.Empty<IQueryDefinition>()),
                new OperationDefinition("foo", OperationType.Query, Enumerable.Empty<ISelection>()));

            // act
            IResolverContext context = ResolverContext.Create(
                services.Object, operationContext, variables.Object,
                q => { });

            // assert
            Assert.Empty(context.Path);
        }

        [Fact]
        public void CreateSelectionContextParentIsNull()
        {
            // arrange
            Mock<IServiceProvider> services = new Mock<IServiceProvider>(MockBehavior.Strict);
            Mock<ISchema> schema = new Mock<ISchema>();
            Mock<IVariableCollection> variables = new Mock<IVariableCollection>(MockBehavior.Strict);            
            OperationContext operationContext = new OperationContext(
                schema.Object, new QueryDocument(Enumerable.Empty<IQueryDefinition>()),
                new OperationDefinition("foo", OperationType.Query, Enumerable.Empty<ISelection>()));
            IResolverContext context = ResolverContext.Create(
                services.Object, operationContext, variables.Object, q => { });

            SelectionContext selectionContext = new SelectionContext(
                new ObjectTypeDefinition("Foo", Enumerable.Empty<FieldDefinition>()),
                new FieldDefinition("foo", NamedType.String),
                new Field("foo"));

            // act
            context = context.Create(selectionContext, null);

            // assert
            Assert.Single(context.Path);
            Assert.Null(context.Parent<string>());
        }

        [Fact]
        public void CreateSelectionContextParentIsFooString()
        {
            // arrange
            Mock<IServiceProvider> services = new Mock<IServiceProvider>(MockBehavior.Strict);
            Mock<ISchema> schema = new Mock<ISchema>();
            Mock<IVariableCollection> variables = new Mock<IVariableCollection>(MockBehavior.Strict);                        
            OperationContext operationContext = new OperationContext(
                schema.Object, new QueryDocument(Enumerable.Empty<IQueryDefinition>()),
                new OperationDefinition("foo", OperationType.Query, Enumerable.Empty<ISelection>()));
            IResolverContext context = ResolverContext.Create(
                services.Object, operationContext, variables.Object, q => { });

            SelectionContext selectionContext = new SelectionContext(
                new ObjectTypeDefinition("Foo", Enumerable.Empty<FieldDefinition>()),
                new FieldDefinition("foo", NamedType.String),
                new Field("foo"));

            // act
            context = context.Create(selectionContext, "Foo");

            // assert
            Assert.Single(context.Path);
            Assert.Equal("Foo", context.Parent<string>());
        }
    }
}