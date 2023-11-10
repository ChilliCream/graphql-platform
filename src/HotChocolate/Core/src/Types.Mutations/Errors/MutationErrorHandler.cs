namespace HotChocolate.Types;

/// <summary>
/// This abstract class can be used to configure mutation errors.
/// </summary>
public abstract class MutationErrorConfiguration
{
    /// <summary>
    /// Allows to register errors with a mutation.
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