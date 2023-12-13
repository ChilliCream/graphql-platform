using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Composition.Properties.CompositionResources;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents the runtime value of 
/// `directive @node(types: [Name!]!, subgraph: Name!) repeatable on SCHEMA`.
/// </summary>
internal sealed class NodeDirective
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeDirective"/> class.
    /// </summary>
    /// <param name="types">The list of node types.</param>
    /// <param name="subgraph">The name of the subgraph.</param>
    public NodeDirective(IReadOnlyList<string> types, string subgraph)
    {
        ArgumentException.ThrowIfNullOrEmpty(subgraph);
        if (types == null || types.Count == 0)
        {
            throw new ArgumentException(NodeDirective_TypesCannotBeNullOrEmpty, nameof(types));
        }
        
        Types = types;
        Subgraph = subgraph;
    }

    /// <summary>
    /// Gets the list of node types.
    /// </summary>
    public IReadOnlyList<string> Types { get; }

    /// <summary>
    /// Gets the name of the subgraph.
    /// </summary>
    public string Subgraph { get; }

    /// <summary>
    /// Creates a <see cref="Directive"/> from this <see cref="NodeDirective"/>.
    /// </summary>
    public Directive ToDirective(IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new Directive(
            context.NodeDirective,
            new Argument(TypesArg, new ListValueNode(Types.Select(t => (IValueNode)new StringValueNode(t)).ToArray())),
            new Argument(SubgraphArg, Subgraph));
    }

    /// <summary>
    /// Tries to parse a <see cref="NodeDirective"/> from a <see cref="Directive"/>.
    /// </summary>
    public static bool TryParse(
        Directive directiveNode,
        IFusionTypeContext context,
        [NotNullWhen(true)] out NodeDirective? directive)
    {
        ArgumentNullException.ThrowIfNull(directiveNode);
        ArgumentNullException.ThrowIfNull(context);

        if (!directiveNode.Name.EqualsOrdinal(context.NodeDirective.Name))
        {
            directive = null;
            return false;
        }

        var types = directiveNode.Arguments.GetValueOrDefault(TypesArg);

        if (types?.Kind is SyntaxKind.StringValue)
        {
            types = new ListValueNode(new[] { types });    
        }
        else if(types?.Kind is not SyntaxKind.ListValue)
        {
            var list = (ListValueNode)types!;
            
            if(list.Items.Count == 0)
            {
                directive = null;
                return false; 
            }
            
            for(var i= 0; i < list.Items.Count; i++)
            {
                if (list.Items[i].Kind is SyntaxKind.StringValue)
                {
                    continue;
                }
                
                directive = null;
                return false;
            }
        }
        else
        {
            directive = null;
            return false;
        }
        
        var subgraph = directiveNode.Arguments.GetValueOrDefault(SubgraphArg)?.ExpectStringLiteral();

        if (subgraph is null)
        {
            directive = null;
            return false;
        }

        var typesList = (ListValueNode)types!;
        var nodeTypes = new string[typesList.Items.Count];

        for (var i = 0; i < typesList.Items.Count; i++)
        {
            nodeTypes[i] = typesList.Items[i].ExpectStringLiteral().Value;
        }

        directive = new NodeDirective(nodeTypes, subgraph.Value);
        return true;
    }

    /// <summary>
    /// Creates the node directive type.
    /// </summary>
    public static DirectiveType CreateType()
    {
        var nameType = new MissingType(FusionTypeBaseNames.Name);
        var nameListType = new ListType(new NonNullType(nameType));
        
        var directiveType = new DirectiveType(FusionTypeBaseNames.NodeDirective)
        {
            Locations = DirectiveLocation.Schema,
            IsRepeatable = true,
            Arguments =
            {
                new InputField(TypesArg, new NonNullType(nameListType)),
                new InputField(SubgraphArg, new NonNullType(nameType))
            },
            ContextData =
            {
                [WellKnownContextData.IsFusionType] = true
            }
        };

        return directiveType;
    }
}
