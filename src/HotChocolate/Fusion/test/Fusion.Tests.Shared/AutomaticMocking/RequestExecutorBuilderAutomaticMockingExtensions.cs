using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public static class RequestExecutorBuilderAutomaticMockingExtensions
{
    public static IRequestExecutorBuilder AddResolverMocking(this IRequestExecutorBuilder builder)
    {
        return builder
            .UseField<MockFieldMiddleware>()
            .TryAddTypeInterceptor<AbstractTypeMockingTypeInterceptor>();
    }

    public static IRequestExecutorBuilder AddTestDirectives(this IRequestExecutorBuilder builder)
    {
        return builder
            .AddDirectiveType(new DirectiveType<ErrorDirective>())
            .AddDirectiveType(new DirectiveType<NullDirective>())
            .AddDirectiveType(new DirectiveType<ReturnsDirective>());
    }
}
