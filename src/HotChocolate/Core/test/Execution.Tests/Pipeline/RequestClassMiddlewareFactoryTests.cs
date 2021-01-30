using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Utilities;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotChocolate.Execution.Pipeline
{
    public class RequestClassMiddlewareFactoryTests
    {
        [Fact]
        public async Task Create_CoreMiddleware_InjectOptimizers()
        {
            // arrange
            RequestCoreMiddleware middleware = RequestClassMiddlewareFactory
                .Create<StubMiddleware<IEnumerable<ISelectionOptimizer>>>();
            var applicationServices = new ServiceCollection().BuildServiceProvider();
            ServiceProvider schemaServices = new ServiceCollection()
                .AddSingleton<ISelectionOptimizer, StubOptimizer>()
                .BuildServiceProvider();
            var schemaName = "_Default";
            IRequestExecutorOptionsAccessor optionsAccessor = new RequestExecutorOptions();
            var factoryContext = new RequestCoreMiddlewareContext(
                schemaName,
                applicationServices,
                schemaServices,
                optionsAccessor);
            var context = new RequestContext(
                new Mock<ISchema>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<IErrorHandler>().Object,
                new Mock<ITypeConverter>().Object,
                new Mock<IActivator>().Object,
                new Mock<IDiagnosticEvents>().Object,
                new Mock<IQueryRequest>().Object);

            // act
            RequestDelegate compiledMiddleware = middleware(factoryContext, context => default);
            await compiledMiddleware(context);


            // assert
            Assert.Single(
                (context.ContextData["result"] as IEnumerable<ISelectionOptimizer>)!);
        }

        public class StubMiddleware<T>
        {
            private readonly RequestDelegate _next;
            private readonly T _injectedValue;

            public StubMiddleware(
                RequestDelegate next,
                T injectedValue)
            {
                _next = next ??
                    throw new ArgumentNullException(nameof(next));
                _injectedValue = injectedValue;
            }

            public ValueTask InvokeAsync(IRequestContext context)
            {
                context.ContextData["result"] = _injectedValue;
                return default;
            }
        }

        public class StubOptimizer : ISelectionOptimizer
        {
            public void OptimizeSelectionSet(SelectionOptimizerContext context)
            {
                throw new NotImplementedException();
            }

            public bool AllowFragmentDeferral(
                SelectionOptimizerContext context,
                InlineFragmentNode fragment)
            {
                throw new NotImplementedException();
            }

            public bool AllowFragmentDeferral(
                SelectionOptimizerContext context,
                FragmentSpreadNode fragmentSpread,
                FragmentDefinitionNode fragmentDefinition)
            {
                throw new NotImplementedException();
            }
        }
    }
}
