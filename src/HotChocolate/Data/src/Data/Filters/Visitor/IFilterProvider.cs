using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
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
    /// Creates a middleware that represents the filter execution logic
    /// for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntityType">
    /// The entity type for which an filter executor shall be created.
    /// </typeparam>
    /// <returns>
    /// Returns a field middleware which represents the filter execution logic
    /// for the specified entity type.
    /// </returns>
    FieldMiddleware CreateExecutor<TEntityType>(string argumentName);

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
