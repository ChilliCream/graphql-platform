using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Spatial.Transformation
{
    internal static class SpatialCompletionContextExtensions
    {
        public static bool IsType<T>(
            this ITypeCompletionContext context,
            ITypeReference typeReference)
            where T : IType
        {
            return context.TryGetType<IType>(typeReference, out var type) &&
                type.NamedType() is T;
        }
    }
}
