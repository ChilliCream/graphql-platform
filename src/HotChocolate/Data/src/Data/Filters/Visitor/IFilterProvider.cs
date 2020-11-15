using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
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
        FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName);

        /// <summary>
        /// Configures the field where the filters are applied. This can be used to add context
        /// data to the field.
        /// </summary>
        void ConfigureField(NameString argumentName, IObjectFieldDescriptor descriptor);
    }
}
