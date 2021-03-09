#nullable enable
using System.Collections.Generic;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Data.Projections;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Projections
{
    /// <inheritdoc/>
    public class Neo4JProjectionVisitorContext
        : ProjectionVisitorContext<MapExpression>
    {
        /// <inheritdoc/>
        public Neo4JProjectionVisitorContext(
            IResolverContext context,
            IOutputType initialType)
            : base(context, initialType, new Neo4JProjectionScope())
        {
        }

        public bool IsRelationship { get; set; } = false;

        public Node? ParentNode { get; set; }

        public Node? CurrentNode { get; set; }
        public Relationship? Relationship { get; set; }

        public List<object> RelationshipProjections { get; } = new();

        /// <summary>
        /// A list of already projected fields
        /// </summary>
        public List<object> Projections { get; } = new();
    }
}
