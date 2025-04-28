using static HotChocolate.Types.ErrorContextDataKeys;
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
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (errorType is null)
        {
            throw new ArgumentNullException(nameof(errorType));
        }

        if (!context.ContextData.ContainsKey(ErrorConventionEnabled))
        {
            throw SchemaErrorBuilder.New()
                .SetMessage(ErrorConventionDisabled_Message, errorType.Name, configuration.Name)
                .BuildException();
        }

        var definitions = ErrorFactoryCompiler.Compile(errorType);

        if (!configuration.ContextData.TryGetValue(ErrorConfigurations, out var value) ||
            value is not List<ErrorConfiguration> errorFactories)
        {
            errorFactories = [];
            configuration.ContextData[ErrorConfigurations] = errorFactories;
        }

        errorFactories.AddRange(definitions);

        foreach (var definition in definitions)
        {
            var typeRef = context.TypeInspector.GetTypeRef(definition.SchemaType);
            configuration.Dependencies.Add(new TypeDependency(typeRef));
        }
    }
}
