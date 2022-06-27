using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;
using Moq;
using Xunit;
using static HotChocolate.Execution.Pipeline.RequestClassMiddlewareFactory;

namespace HotChocolate.Execution.Pipeline;

public class RequestClassMiddlewareFactoryTests
{
    [Fact]
    public async Task Create_CoreMiddleware_InjectOptimizers()
    {
        // arrange
        var middleware = Create<StubMiddleware<IEnumerable<ISelectionSetOptimizer>>>();
        var applicationServices = new ServiceCollection().BuildServiceProvider();
        var schemaServices = new ServiceCollection()
            .AddSingleton<ISelectionSetOptimizer, StubOptimizer>()
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
            1,
            new Mock<IErrorHandler>().Object,
            new Mock<ITypeConverter>().Object,
            new Mock<IActivator>().Object,
            new Mock<IExecutionDiagnosticEvents>().Object);

        context.Initialize(
            new Mock<IQueryRequest>().Object,
            new Mock<IServiceProvider>().Object);

        // act
        var compiledMiddleware = middleware(factoryContext, _ => default);
        await compiledMiddleware(context);

        // assert
        Assert.Single((context.ContextData["result"] as IEnumerable<ISelectionSetOptimizer>)!);
    }

    private sealed class StubMiddleware<T>
    {
        private readonly RequestDelegate _next;
        private readonly T _injectedValue;

        public StubMiddleware(
            RequestDelegate next,
            T injectedValue)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _injectedValue = injectedValue;
        }

        public ValueTask InvokeAsync(IRequestContext context)
        {
            context.ContextData["result"] = _injectedValue;
            return _next(context);
        }
    }

    private sealed class StubOptimizer : ISelectionSetOptimizer
    {
        public void OptimizeSelectionSet(SelectionSetOptimizerContext context)
        {
            throw new NotImplementedException();
        }
    }
}
