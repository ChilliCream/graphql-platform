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

        public int CurrentLevel { get; set; } = 0;
        public Stack<Node> StartNodes { get; } = new();
        public Stack<Node> EndNodes { get; } = new();
        public Stack<Relationship> Relationships { get; } = new();
        public Stack<Neo4JRelationshipAttribute> RelationshipTypes { get; } = new();
        public Dictionary<int, Queue<object>> RelationshipProjections { get; } = new();

        /// <summary>
        /// A list of already projected fields
        /// </summary>
        public List<object> Projections { get; } = new();
    }
}
