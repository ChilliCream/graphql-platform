using System.Linq;

namespace HotChocolate.Types
{
    public static class DirectiveCollectionExtensions
    {
        public static T SingleOrDefault<T>(this IDirectiveCollection directives) =>
            directives
                .Where(t => typeof(T).IsAssignableFrom(t.Type.RuntimeType))
                .Select(t => t.ToObject<T>()).SingleOrDefault();
    }
}
