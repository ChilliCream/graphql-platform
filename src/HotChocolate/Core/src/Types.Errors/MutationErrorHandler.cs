namespace HotChocolate.Types;

/// <summary>
/// This abstract class can be used to configure mutation errors.
/// </summary>
public abstract class MutationErrorConfiguration
{
    /// <summary>
    /// Override to register error dependencies.
    /// </summary>
    /// <param name="context">
    /// The descriptor context.
    /// </param>
    /// <returns>
    /// Returns the dependency references.
    /// </returns>
    public virtual IEnumerable<TypeReference> OnResolveDependencies(
        IDescriptorContext context)
        => Array.Empty<TypeReference>();

    /// <summary>
    /// Override to register errors with a mutation.
    /// </summary>
    /// <param name="context">
    /// The descriptor context.
    /// </param>
    /// <param name="mutationField">
    /// The mutation field.
    /// </param>
    public abstract void OnConfigure(
        IDescriptorContext context,
        ObjectFieldDefinition mutationField);
}
