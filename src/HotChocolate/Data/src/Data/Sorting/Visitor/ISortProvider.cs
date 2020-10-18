using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting
{
    public interface ISortProvider
    {
        IReadOnlyCollection<ISortFieldHandler> FieldHandlers { get; }

        IReadOnlyCollection<ISortOperationHandler> OperationHandlers { get; }

        FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName);

        /// <summary>
        /// Configures the field where the filters are applied. This can be used to add context
        /// data to the field.
        /// </summary>
        void ConfigureField(NameString argumentName, IObjectFieldDescriptor descriptor);
    }
}

