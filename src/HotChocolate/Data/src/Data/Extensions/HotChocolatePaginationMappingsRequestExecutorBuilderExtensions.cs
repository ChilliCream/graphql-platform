using GreenDonut.Data;
using GreenDonut.Data.Cursors;
using GreenDonut.Data.Cursors.Serializers;
using HotChocolate.Data.Pagination;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
/// </summary>
public static class HotChocolatePaginationMappingsRequestExecutorBuilderExtensions
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
    {
        builder.Services.AddSingleton<IParameterExpressionBuilder, PagingArgumentsParameterExpressionBuilder>();
        return builder;
    }

    /// <summary>
    /// Registers a <see cref="ICursorKeySerializer"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="serializer">
    /// The <see cref="ICursorKeySerializer"/> to register.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    public static IRequestExecutorBuilder AddCursorKeySerializer(
        this IRequestExecutorBuilder builder,
        ICursorKeySerializer serializer)
    {
        CursorKeySerializerRegistration.Register(serializer);
        return builder;
    }
}
