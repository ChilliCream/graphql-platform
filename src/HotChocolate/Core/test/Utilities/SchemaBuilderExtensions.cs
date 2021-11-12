using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Tests;

public static class SchemaBuilderExtensions
{
    public static IRequestExecutorBuilder UseNothing(
        this IRequestExecutorBuilder builder) =>
        builder.UseField(next => context => default);
}
