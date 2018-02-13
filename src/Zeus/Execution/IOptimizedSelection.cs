using System.Collections.Generic;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    public interface IOptimizedSelection
    {
        string Name { get; }

        ObjectTypeDefinition TypeDefinition { get; }

        FieldDefinition FieldDefinition { get; }

        Field Field { get; }

        IResolver Resolver { get; }

        IReadOnlyCollection<IOptimizedSelection> Selections { get; }

        IResolverContext CreateContext(IResolverContext parentContext, object parentResult);
    }
}