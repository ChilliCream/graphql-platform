using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public interface IDirectiveContext
    {
        IDirective Directive { get; }


        /// <summary>
        /// Gets a specific directive argument.
        /// </summary>
        /// <param name="name">
        /// The argument name.
        /// </param>
        /// <typeparam name="T">
        /// The type to which the argument shall be casted to.
        /// </typeparam>
        /// <returns>
        /// Returns a specific field argument.
        /// </returns>
        T Argument<T>(string name);

        IReadOnlyCollection<FieldSelection> CollectFields();

        Task<T> ResolveFieldAsync<T>();
    }
}
