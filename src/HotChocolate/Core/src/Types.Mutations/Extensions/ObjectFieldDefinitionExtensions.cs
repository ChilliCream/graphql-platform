using static HotChocolate.Types.ErrorContextDataKeys;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Provides extensions to the <see cref="ObjectFieldDefinition"/> for the mutation convention.
/// </summary>
public static class ObjectFieldDefinitionExtensions
{
    /// <summary>
    /// Registers an error type with this field for the mutation convention
    /// to pick up.
    /// </summary>
    /// <param name="fieldDefinition">The object field definition.</param>
    /// <param name="descriptorContext">The descriptor context.</param>
    /// <param name="errorType">
    /// The type of the exception, the class with factory methods or the error with an exception
    /// as the argument. See the examples in <see cref="Error"/>.
    /// </param>
    public static void AddErrorType(
        this ObjectFieldDefinition fieldDefinition,
        IDescriptorContext descriptorContext,
        Type errorType)
    {
        if (fieldDefinition is null)
        {
            throw new ArgumentNullException(nameof(fieldDefinition));
        }

        if (descriptorContext is null)
        {
            throw new ArgumentNullException(nameof(descriptorContext));
        }

        if (errorType is null)
        {
            throw new ArgumentNullException(nameof(errorType));
        }

        if (descriptorContext.ContextData[MutationContextDataKeys.Options] == null)
        {
            throw new Exception("Mutation convention isn't turned on");
            // TODO: Proper exception, or maybe we just let this through?
        }

        var definitions = ErrorFactoryCompiler.Compile(errorType);

        if (!fieldDefinition.ContextData.TryGetValue(ErrorDefinitions, out var value) ||
            value is not List<ErrorDefinition> errorFactories)
        {
            errorFactories = new List<ErrorDefinition>();
            fieldDefinition.ContextData[ErrorDefinitions] = errorFactories;
        }

        errorFactories.AddRange(definitions);

        foreach (var definition in definitions)
        {
            var typeRef = descriptorContext.TypeInspector.GetTypeRef(definition.SchemaType);
            fieldDefinition.Dependencies.Add(new TypeDependency(typeRef));
        }
    }
}
