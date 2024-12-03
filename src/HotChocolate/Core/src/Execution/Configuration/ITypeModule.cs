using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Execution.Configuration;

/// <summary>
/// A type module allows you to easily build a component that dynamically provides types to
/// the schema building process.
/// </summary>
public interface ITypeModule
{
    /// <summary>
    /// This event signals that types have changed and the current schema
    /// version has to be phased out.
    /// </summary>
    event EventHandler<EventArgs> TypesChanged;

    /// <summary>
    /// Will be called by the schema building process to add the dynamically created
    /// types and type extensions to the schema building process.
    /// </summary>
    /// <param name="context">
    /// The descriptor context provides access to schema building services and conventions.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a collection of types and type extensions that shall be
    /// added to the schema building process.
    /// </returns>
    ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken);
}

/// <summary>
/// A type module allows you to easily build a component that dynamically provides types to
/// the schema building process.
/// </summary>
public abstract class TypeModule : ITypeModule
{
    /// <summary>
    /// This event signals that types have changed and the current schema
    /// version has to be phased out.
    /// </summary>
    public event EventHandler<EventArgs>? TypesChanged;

    internal virtual ValueTask ConfigureAsync(
        ConfigurationContext context,
        CancellationToken cancellationToken)
        => default;

    /// <summary>
    /// Will be called by the schema building process to add the dynamically created
    /// types and type extensions to the schema building process.
    /// </summary>
    /// <param name="context">
    /// The descriptor context provides access to schema building services and conventions.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a collection of types and type extensions that shall be
    /// added to the schema building process.
    /// </returns>
    public virtual ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken)
        => new(Array.Empty<ITypeSystemMember>());

    /// <summary>
    /// Signals that types have changed and the current schema version has to be phased out.
    /// </summary>
    protected void OnTypesChanged()
        => TypesChanged?.Invoke(this, EventArgs.Empty);
}
