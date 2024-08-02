using HotChocolate.Configuration;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

/// <summary>
/// A <see cref="IFilterProvider"/> translates a incoming query to another
/// object structure at runtime
/// </summary>
public interface IFilterProvider
{
    /// <summary>
    /// A collection of all <see cref="IFilterFieldHandler"/> that this provider knows.
    /// </summary>
    IReadOnlyCollection<IFilterFieldHandler> FieldHandlers { get; }

    /// <summary>
    /// Creates a query builder that builds up the filter clause.
    /// </summary>
    /// <typeparam name="TEntityType">
    /// The entity type for which query builder shall be created.
    /// </typeparam>
    /// <returns>
    /// Returns a query builder that builds up the filter clause.
    /// </returns>
    IQueryBuilder CreateBuilder<TEntityType>(string argumentName);

    /// <summary>
    /// Configures the field where the filters are applied. This can be used to add context
    /// data to the field.
    /// </summary>
    void ConfigureField(string argumentName, IObjectFieldDescriptor descriptor);

    /// <summary>
    /// Creates metadata for a field that the provider can pick up an use for the translation
    /// </summary>
    IFilterMetadata? CreateMetaData(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition);
}
