using System;
using HotChocolate.Utilities;
using Moq;

namespace HotChocolate.Execution
{
    public static class MiddlewareTools
    {
        public static IRequestServiceScope CreateEmptyRequestServiceScope()
        {
            var disposable = new Mock<IDisposable>();
            return new RequestServiceScope(
                new EmptyServiceProvider(),
                disposable.Object);
        }

        public static IRequestServiceScope CreateRequestServiceScope(
            this IServiceProvider serviceProvider)
        {
            var disposable = new Mock<IDisposable>();
            return new RequestServiceScope(
                serviceProvider,
                disposable.Object);
        }
    }
}
