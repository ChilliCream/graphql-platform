using HotChocolate.Features;
using static HotChocolate.Types.Properties.ErrorResources;

namespace HotChocolate.Types;

/// <summary>
/// Provides extensions to the <see cref="ObjectFieldConfiguration"/> for the mutation convention.
/// </summary>
public static class ErrorObjectFieldConfigurationExtensions
{
    /// <summary>
    /// Registers an error type with this field for the mutation convention
    /// to pick up.
    /// </summary>
    /// <param name="configuration">The object field definition.</param>
    /// <param name="context">The descriptor context.</param>
    /// <param name="errorType">
    /// The type of the exception, the class with factory methods or the error with an exception
    /// as the argument. See the examples in <see cref="Error"/>.
    /// </param>
    public static void AddErrorType(
        this ObjectFieldConfiguration configuration,
        IDescriptorContext context,
        Type errorType)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(errorType);

        if (!context.Features.TryGet<ErrorSchemaFeature>(out var _))
        {
            throw SchemaErrorBuilder.New()
                .SetMessage(ErrorConventionDisabled_Message, errorType.Name, configuration.Name)
                .BuildException();
        }

        var errorConfigs = ErrorFactoryCompiler.Compile(errorType);
        var feature = configuration.Features.GetOrSet<ErrorFieldFeature>();
        feature.ErrorConfigurations.AddRange(errorConfigs);

        foreach (var errorConfig in errorConfigs)
        {
            var typeRef = context.TypeInspector.GetTypeRef(errorConfig.SchemaType);
            configuration.Dependencies.Add(new TypeDependency(typeRef));
        }
    }
}
