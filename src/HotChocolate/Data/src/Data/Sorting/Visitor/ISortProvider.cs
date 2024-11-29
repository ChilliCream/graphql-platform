using HotChocolate.Configuration;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public interface ISortProvider
{
    IReadOnlyCollection<ISortFieldHandler> FieldHandlers { get; }

    IReadOnlyCollection<ISortOperationHandler> OperationHandlers { get; }

    /// <summary>
    /// Creates a query builder that builds up the order clause.
    /// </summary>
    /// <typeparam name="TEntityType">
    /// The entity type for which query builder shall be created.
    /// </typeparam>
    /// <returns>
    /// Returns a query builder that builds up the order clause.
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
    ISortMetadata? CreateMetaData(
        ITypeCompletionContext context,
        ISortInputTypeDefinition typeDefinition,
        ISortFieldDefinition fieldDefinition);
}
