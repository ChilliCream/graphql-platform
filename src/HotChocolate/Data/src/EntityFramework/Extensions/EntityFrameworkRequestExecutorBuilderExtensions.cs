using HotChocolate.Data;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class EntityFrameworkRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder RegisterDbContextFactory<T>(
        this IRequestExecutorBuilder builder)
        where T : DbContext
    {
        builder.Services.AddSingleton<IParameterExpressionBuilder, ContextFactoryParameterExpressionBuilder<T>>();
        return builder;
    }

    public static IRequestExecutorBuilder AddDbContextCursorPagingProvider(
        this IRequestExecutorBuilder builder,
        string? providerName = null,
        bool defaultProvider = false)
        => builder.AddCursorPagingProvider<EfQueryableCursorPagingProvider>(providerName, defaultProvider);
}
