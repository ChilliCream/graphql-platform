using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class MutationRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddMutations(this IRequestExecutorBuilder builder)
    {
        builder.TryAddTypeInterceptor<ErrorTypeInterceptor>();
        builder.TryAddTypeInterceptor<InputArgumentTypeInterceptor>();
        builder.TryAddTypeInterceptor<PayloadTypeInterceptor>();
        builder.Services.AddSingleton<IParameterExpressionBuilder, InputParameterExpressionBuilder>();

        return builder;
    }

    /// <summary>
    /// Defines the common interface that all errors implement.
    /// To specify the interface you can either provide a interface runtime type or a HotChocolate
    /// interface schema type.
    ///
    /// This has to be used together with <see cref="ErrorAttribute"/>  or
    /// <see cref="ErrorObjectFieldDescriptorExtensions.Error"/>
    /// </summary>
    /// <param name="builder">
    /// The request executor builder
    /// </param>
    /// <typeparam name="T">
    /// The type that is used as the common interface
    /// </typeparam>
    /// <returns>j
    /// The schema builder
    /// </returns>
    public static IRequestExecutorBuilder AddErrorInterfaceType<T>(
        this IRequestExecutorBuilder builder) =>
        builder.ConfigureSchema(x => x.AddErrorInterfaceType<T>());

    /// <summary>
    /// Defines the common interface that all errors implement.
    /// To specify the interface you can either provide a interface runtime type or a HotChocolate
    /// interface schema type.
    ///
    /// This has to be used together with <see cref="ErrorAttribute"/>  or
    /// <see cref="ErrorObjectFieldDescriptorExtensions.Error"/>
    /// </summary>
    /// <param name="builder">
    /// The request executor builder
    /// </param>
    /// <param name="type">
    /// The type that is used as the common interface
    /// </param>
    /// <returns>
    /// The request executor builder
    /// </returns>
    public static IRequestExecutorBuilder AddErrorInterfaceType(
        this IRequestExecutorBuilder builder,
        Type type) =>
        builder.ConfigureSchema(x => x.AddErrorInterfaceType(type));
}
