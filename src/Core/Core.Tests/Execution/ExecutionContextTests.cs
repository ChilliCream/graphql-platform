using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Execution
{
    public class ExecutionContextTests
    {
        [Fact]
        public void ContextDataArePassedOn()
        {
            // arrange
            var schema = Schema.Create(
                "type Query { foo: String }",
                c => c.Use(next => context => Task.CompletedTask));

            var query = Parser.Default.Parse("{ foo }");

            var errorHandler = new Mock<IErrorHandler>();

            var services = new Mock<IServiceProvider>();
            services.Setup(t => t.GetService(typeof(IErrorHandler)))
                .Returns(errorHandler.Object);

            IRequestServiceScope serviceScope =
                services.Object.CreateRequestServiceScope();

            var operation = new Mock<IOperation>();
            operation.Setup(t => t.Query).Returns(query);

            var variables = new Mock<IVariableCollection>();

            var directives = new DirectiveLookup(new Dictionary<ObjectType, IDictionary<FieldNode, IReadOnlyCollection<IDirective>>>());

            var contextData = new Dictionary<string, object>
            {
                { "abc", "123" }
            };

            // act
            var executionContext = new ExecutionContext(
                schema, serviceScope, operation.Object,
                variables.Object, directives, contextData,
                CancellationToken.None);

            // assert
            Assert.True(object.ReferenceEquals(
                contextData, executionContext.ContextData));
        }

        [Fact]
        public void CloneExecutionContext()
        {
            // arrange
            var schema = Schema.Create(
                "type Query { foo: String }",
                c => c.Use(next => context => Task.CompletedTask));

            var query = Parser.Default.Parse("{ foo }");

            var errorHandler = new Mock<IErrorHandler>();

            var services = new Mock<IServiceProvider>();
            services.Setup(t => t.GetService(typeof(IErrorHandler)))
                .Returns(errorHandler.Object);

            IRequestServiceScope serviceScope = services.Object
                .CreateRequestServiceScope();

            var operation = new Mock<IOperation>();
            operation.Setup(t => t.Query).Returns(query);

            var variables = new Mock<IVariableCollection>();

            var directives = new DirectiveLookup(new Dictionary<ObjectType, IDictionary<FieldNode, IReadOnlyCollection<IDirective>>>());

            var contextData = new Dictionary<string, object>
            {
                { "abc", "123" }
            };

            // act
            var executionContext = new ExecutionContext(
                schema, serviceScope, operation.Object,
                variables.Object, directives, contextData,
                CancellationToken.None);
            IExecutionContext cloned = executionContext.Clone();

            // assert
            Assert.Equal(contextData, executionContext.ContextData);
            Assert.Equal(contextData, cloned.ContextData);
            Assert.False(object.ReferenceEquals(
                executionContext.Result, cloned.Result));
            Assert.False(object.ReferenceEquals(
                executionContext.ContextData, cloned.ContextData));
        }
    }
}
