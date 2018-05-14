using System.Collections.Immutable;
using System.Linq;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public interface IResolverContext
    {
        // schema context
        Schema Schema { get; }

        ObjectType ObjectType { get; }

        Field Field { get; }

        // query context
        DocumentNode QueryDocument { get; }

        OperationDefinitionNode Operation { get; }

        FieldNode FieldSelection { get; }

        // execution context
        ImmutableStack<object> Path { get; } // parents

        // string Path => /foo/bar[0]/test

        T Parent<T>();

        T Argument<T>(string name);

        T Service<T>();
    }
}
