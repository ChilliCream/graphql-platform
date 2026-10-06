using System.Collections.Immutable;
using HotChocolate.Fusion.DirectiveMergers;
using HotChocolate.Fusion.Directives;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Features;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion;

/// <summary>
/// Flattens the authorization requirements of the merged schema so that every field carries its
/// complete requirement.
/// </summary>
/// <remarks>
/// Interface type requirements flow down to implementing types. A field combines its own, its
/// declaring type and its return type requirements and shares the result with same-named fields
/// across the implements relation.
/// </remarks>
internal static class AuthorizationInheritance
{
    private static readonly ImmutableSortedSet<string> s_noSources =
        ImmutableSortedSet.Create<string>(StringComparer.Ordinal);

    public static void Apply(
        MutableSchemaDefinition mergedSchema,
        MutableDirectiveDefinition fusionAuthorization,
        IEnumerable<MutableSchemaDefinition> sourceSchemas)
    {
        var sources = sourceSchemas.ToArray();
        var typeNodes = new Dictionary<string, Node>(StringComparer.Ordinal);
        var fieldNodes = new Dictionary<(string Type, string Field), Node>();
        var complexTypes = new List<MutableComplexTypeDefinition>();

        foreach (var type in mergedSchema.Types)
        {
            switch (type)
            {
                case MutableComplexTypeDefinition complexType:
                    complexTypes.Add(complexType);
                    typeNodes[complexType.Name] = CreateNode(
                        new SchemaCoordinate(complexType.Name),
                        complexType.Directives,
                        GetDirectSources(sources, complexType.Name, null));

                    foreach (var field in complexType.Fields)
                    {
                        fieldNodes[(complexType.Name, field.Name)] = CreateNode(
                            new SchemaCoordinate(complexType.Name, field.Name, ofDirective: false),
                            field.Directives,
                            GetDirectSources(sources, complexType.Name, field.Name));
                    }

                    break;

                case MutableEnumTypeDefinition or MutableScalarTypeDefinition:
                    typeNodes[type.Name] = CreateNode(
                        new SchemaCoordinate(type.Name),
                        GetDirectives(type),
                        GetDirectSources(sources, type.Name, null));
                    break;
            }
        }

        if (!typeNodes.Values.Concat(fieldNodes.Values).Any(static n => !n.Own.Auth.IsEmpty))
        {
            return;
        }

        foreach (var complexType in complexTypes)
        {
            foreach (var interfaceType in complexType.Implements)
            {
                foreach (var interfaceField in interfaceType.Fields)
                {
                    if (fieldNodes.TryGetValue((complexType.Name, interfaceField.Name), out var impl))
                    {
                        Link(impl, fieldNodes[(interfaceType.Name, interfaceField.Name)]);
                    }
                }
            }
        }

        foreach (var complexType in complexTypes)
        {
            var typeNode = typeNodes[complexType.Name];

            foreach (var interfaceType in complexType.Implements)
            {
                typeNode.Closed = typeNode.Closed.And(typeNodes[interfaceType.Name].Own);
            }
        }

        foreach (var complexType in complexTypes)
        {
            var declaring = typeNodes[complexType.Name].Closed;

            foreach (var field in complexType.Fields)
            {
                var node = fieldNodes[(complexType.Name, field.Name)];
                var returned = typeNodes.TryGetValue(field.Type.NamedType().Name, out var returnNode)
                    ? returnNode.Closed
                    : Tracked.Empty;

                node.Base = node.Own.And(declaring).And(returned);
            }
        }

        Close(fieldNodes.Values, static n => n.Base);

        var metadata = new AuthorizationInheritanceMetadata();

        foreach (var complexType in complexTypes)
        {
            var typeNode = typeNodes[complexType.Name];

            Write(typeNode, fusionAuthorization);

            if (typeNode.Own.Auth.IsEmpty && !typeNode.Closed.Auth.IsEmpty)
            {
                ReportType(metadata, typeNode, complexType, typeNodes);
            }

            foreach (var field in complexType.Fields)
            {
                var fieldNode = fieldNodes[(complexType.Name, field.Name)];

                Write(fieldNode, fusionAuthorization);

                if (fieldNode.Own.Auth.IsEmpty && !fieldNode.Closed.Auth.Matches(fieldNode.Base.Auth))
                {
                    ReportField(metadata, fieldNode);
                }
            }
        }

        if (metadata.Entries.Count > 0)
        {
            mergedSchema.Features.Set(metadata);
        }
    }

    private static Node CreateNode(
        SchemaCoordinate coordinate,
        DirectiveCollection directives,
        ImmutableSortedSet<string> directSources)
    {
        var directive = directives.FirstOrDefault(DirectiveNames.FusionAuthorization);
        var auth = directive is null
            ? MergedAuthorization.Empty
            : AuthorizationGroups.FromFusionDirective(directive);

        return new Node(coordinate, directives, new Tracked(auth, auth.IsEmpty ? s_noSources : directSources));
    }

    private static DirectiveCollection GetDirectives(ITypeDefinition type)
    {
        return type switch
        {
            MutableEnumTypeDefinition e => e.Directives,
            MutableScalarTypeDefinition s => s.Directives,
            _ => throw new InvalidOperationException()
        };
    }

    private static void Link(Node left, Node right)
    {
        left.Neighbors.Add(right);
        right.Neighbors.Add(left);

        var leftRoot = left.Find();
        var rightRoot = right.Find();

        if (!ReferenceEquals(leftRoot, rightRoot))
        {
            rightRoot.Parent = leftRoot;
        }
    }

    // Every node of a component receives the combined requirement of the whole component.
    private static void Close(IEnumerable<Node> nodes, Func<Node, Tracked> select)
    {
        var nodeList = nodes.ToList();
        var combined = new Dictionary<Node, Tracked>();

        foreach (var node in nodeList)
        {
            var root = node.Find();
            combined[root] = combined.TryGetValue(root, out var current)
                ? current.And(select(node))
                : select(node);
        }

        foreach (var node in nodeList)
        {
            node.Closed = combined[node.Find()];
        }
    }

    private static void Write(Node node, MutableDirectiveDefinition fusionAuthorization)
    {
        if (node.Closed.Auth.Matches(node.Own.Auth))
        {
            return;
        }

        var directive = AuthorizationDirectiveMerger.CreateDirective(
            fusionAuthorization,
            node.Closed.Auth);
        var existing = node.Directives.FirstOrDefault(DirectiveNames.FusionAuthorization);

        if (existing is null)
        {
            node.Directives.Insert(0, directive);
        }
        else
        {
            node.Directives.Replace(existing, directive);
        }
    }

    private static void ReportType(
        AuthorizationInheritanceMetadata metadata,
        Node member,
        MutableComplexTypeDefinition complexType,
        Dictionary<string, Node> typeNodes)
    {
        var paths = new SortedSet<string>(StringComparer.Ordinal);
        var sourceNames = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var interfaceType in complexType.Implements)
        {
            var origin = typeNodes[interfaceType.Name];

            if (origin.Own.Auth.IsEmpty)
            {
                continue;
            }

            paths.Add(FormatPath([origin, member]));
            sourceNames.UnionWith(origin.Own.Sources);
        }

        if (paths.Count > 0)
        {
            metadata.Entries.Add(
                new InheritedAuthorization(member.Coordinate, [.. paths], [.. sourceNames]));
        }
    }

    private static void ReportField(AuthorizationInheritanceMetadata metadata, Node member)
    {
        var paths = new SortedSet<string>(StringComparer.Ordinal);
        var sourceNames = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var origin in GetComponent(member))
        {
            var tracked = origin.Base;

            if (ReferenceEquals(origin, member)
                || tracked.Auth.IsEmpty
                || member.Base.And(tracked).Auth.Matches(member.Base.Auth))
            {
                continue;
            }

            paths.Add(FormatPath(FindPath(origin, member)));
            sourceNames.UnionWith(tracked.Sources);
        }

        if (paths.Count > 0)
        {
            metadata.Entries.Add(
                new InheritedAuthorization(member.Coordinate, [.. paths], [.. sourceNames]));
        }
    }

    // The members of the connected component, in discovery order from the given member.
    private static List<Node> GetComponent(Node start)
    {
        var visited = new HashSet<Node> { start };
        var queue = new Queue<Node>();
        var result = new List<Node>();

        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            result.Add(node);

            foreach (var neighbor in node.Neighbors)
            {
                if (visited.Add(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        return result;
    }

    private static List<Node> FindPath(Node from, Node to)
    {
        var previous = new Dictionary<Node, Node?> { [from] = null };
        var queue = new Queue<Node>();
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();

            if (ReferenceEquals(node, to))
            {
                break;
            }

            foreach (var neighbor in node.Neighbors)
            {
                if (previous.TryAdd(neighbor, node))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        var path = new List<Node>();

        for (var current = (Node?)to; current is not null; current = previous[current])
        {
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private static string FormatPath(List<Node> path)
        => string.Join(" -> ", path.Select(static n => n.Coordinate.ToString()));

    private static ImmutableSortedSet<string> GetDirectSources(
        MutableSchemaDefinition[] sources,
        string typeName,
        string? fieldName)
    {
        var names = s_noSources.ToBuilder();

        foreach (var schema in sources)
        {
            if (!schema.Types.TryGetType(typeName, out var type)
                || (fieldName is null
                    && type is MutableObjectTypeDefinition standIn
                    && standIn.Directives.ContainsName(DirectiveNames.InterfaceObject)))
            {
                continue;
            }

            IEnumerable<Directive>? directives = null;

            if (fieldName is null)
            {
                directives = type switch
                {
                    MutableComplexTypeDefinition c => c.Directives.AsEnumerable(),
                    MutableEnumTypeDefinition e => e.Directives.AsEnumerable(),
                    MutableScalarTypeDefinition s => s.Directives.AsEnumerable(),
                    _ => null
                };
            }
            else if (type is MutableComplexTypeDefinition complexType
                && complexType.Fields.TryGetField(fieldName, out var field)
                && field is { IsInternal: false, IsOverridden: false })
            {
                directives = field.Directives.AsEnumerable();
            }

            if (directives?.Any(IsSourceAuthorizationDirective) is true)
            {
                names.Add(schema.Name);
            }
        }

        return names.ToImmutable();
    }

    private static bool IsSourceAuthorizationDirective(Directive directive)
        => directive.Name is DirectiveNames.Authenticated
            or DirectiveNames.RequiresScopes
            or DirectiveNames.Policy;

    private readonly record struct Tracked(MergedAuthorization Auth, ImmutableSortedSet<string> Sources)
    {
        public static Tracked Empty { get; } = new(MergedAuthorization.Empty, s_noSources);

        public Tracked And(Tracked other)
        {
            if (other.Auth.IsEmpty)
            {
                return this;
            }

            if (Auth.IsEmpty)
            {
                return other;
            }

            return new Tracked(Auth.And(other.Auth), Sources.Union(other.Sources));
        }
    }

    private sealed class Node(SchemaCoordinate coordinate, DirectiveCollection directives, Tracked own)
    {
        public SchemaCoordinate Coordinate { get; } = coordinate;

        public DirectiveCollection Directives { get; } = directives;

        public Tracked Own { get; } = own;

        public Tracked Base { get; set; } = own;

        public Tracked Closed { get; set; } = own;

        public List<Node> Neighbors { get; } = [];

        public Node? Parent { get; set; }

        public Node Find()
        {
            var root = this;

            while (root.Parent is not null)
            {
                root = root.Parent;
            }

            return root;
        }
    }
}
