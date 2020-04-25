using System.Threading.Tasks;
using Moq;
using Xunit;

namespace HotChocolate.Execution.Utilities
{
    public class ClassMiddlewareFactoryTests
    {
        // [Fact]
        public void Create_Middleware_From_Class_That_Implements_IRequestMiddleware()
        {
            RequestMiddleware middleware =
                ClassMiddlewareFactory.Create<CustomMiddlewareThatImplementsInterface>();
            var contextMock = new Mock<IRequestContext>();
            middleware.Invoke(c => Task.CompletedTask).Invoke(contextMock.Object);
        }

        public class CustomMiddlewareThatImplementsInterface : IRequestMiddleware
        {
            public Task InvokeAsync(IRequestContext context, RequestDelegate next)
            {
                return Task.CompletedTask;
            }
        }
    }
}
