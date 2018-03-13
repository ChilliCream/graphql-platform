using System.Collections.Generic;
using Prometheus.Abstractions;
using Prometheus.Resolvers;

namespace Prometheus.Execution
{
    public interface IOptimizedSelection
    {
        string Name { get; }

        ObjectTypeDefinition TypeDefinition { get; }

        FieldDefinition FieldDefinition { get; }

        Field Field { get; }

        ResolverDelegate Resolver { get; }

        IEnumerable<IOptimizedSelection> GetSelections(IType type);

        IResolverContext CreateContext(IResolverContext parentContext, object parentResult);
    }
}