using HotChocolate.Execution.Configuration;
using HotChocolate.ModelContextProtocol.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ModelContextProtocol.Extensions;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddMcp(this IRequestExecutorBuilder builder)
    {
        builder.AddDirectiveType<McpToolAnnotationsDirectiveType>();

        return builder;
    }
}
