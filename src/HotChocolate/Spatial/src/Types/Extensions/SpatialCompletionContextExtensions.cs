using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Spatial
{
    /// <summary>
    /// Common extension of the <see cref="ITypeCompletionContext"/>
    /// </summary>
    internal static class SpatialCompletionContextExtensions
    {
        /// <summary>
        /// Checks if the named type of the type reference is of type <typeparamref name="T"/>
        /// </summary>
        /// <param name="context">The completion context</param>
        /// <param name="typeReference">The type reference</param>
        /// <typeparam name="T">The type of the named type</typeparam>
        /// <returns><c>true</c> when the type reference is of <typeparamref name="T"/></returns>
        public static bool IsNamedType<T>(
            this ITypeCompletionContext context,
            ITypeReference typeReference)
            where T : IType
        {
            return context.TryGetType<IType>(typeReference, out var type) &&
                type.NamedType() is T;
        }
    }
}
