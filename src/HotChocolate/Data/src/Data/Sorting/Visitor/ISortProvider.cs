using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public interface ISortProvider
{
    IReadOnlyCollection<ISortFieldHandler> FieldHandlers { get; }

    IReadOnlyCollection<ISortOperationHandler> OperationHandlers { get; }

    FieldMiddleware CreateExecutor<TEntityType>(string argumentName);

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

