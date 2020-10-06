using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public interface IFilterProvider
    {
        IReadOnlyCollection<IFilterFieldHandler> FieldHandlers { get; }

        FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName);

        /// <summary>
        /// Configures the field where the filters are applied. This can be used to add context
        /// data to the field.
        /// </summary>
        void ConfigureField(NameString argumentName, IObjectFieldDescriptor descriptor);
    }
}
