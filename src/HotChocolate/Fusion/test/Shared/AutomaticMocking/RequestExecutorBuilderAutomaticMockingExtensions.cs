using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Shared;

public static class RequestExecutorBuilderAutomaticMockingExtensions
{
    public static IRequestExecutorBuilder AddResolverMocking(this IRequestExecutorBuilder builder)
    {
        return builder.UseField<MockFieldMiddleware>();
    }

    public static IRequestExecutorBuilder AddTestDirectives(this IRequestExecutorBuilder builder)
    {
        return builder
            .AddDirectiveType(new DirectiveType<ErrorDirective>())
            .AddDirectiveType(new DirectiveType<NullDirective>());
    }
}
