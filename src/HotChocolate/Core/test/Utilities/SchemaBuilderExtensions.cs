using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;

namespace HotChocolate.Tests;

public static class SchemaBuilderExtensions
{
    public static IRequestExecutorBuilder UseNothing(
        this IRequestExecutorBuilder builder) =>
        builder.UseField(next => context => default);
}
