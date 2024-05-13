using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Pagination;
using HotChocolate.Pagination;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
/// </summary>
public static class EntityFrameworkRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds resolver compiler mapping for the <see cref="PagingArguments"/> from the EFCore helper lib.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    public static IRequestExecutorBuilder AddPagingArguments(
        this IRequestExecutorBuilder builder)
        => builder.AddParameterExpressionBuilder(
            context => MapArguments(
                context.GetLocalState<CursorPagingArguments>(
                    WellKnownContextData.PagingArguments)));

    private static PagingArguments MapArguments(CursorPagingArguments arguments)
        => new(arguments.First, arguments.After, arguments.Last, arguments.Before);
}

