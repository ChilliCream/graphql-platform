using System.Text.Json;
using HotChocolate.Execution.Internal;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using static HotChocolate.Fusion.FusionResources;
using static HotChocolate.Fusion.Utilities.Utf8QueryPlanPropertyNames;
using static HotChocolate.Fusion.Utilities.ErrorHelper;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// The resolver node is responsible for fetching nodes from subgraphs.
/// </summary>
/// <param name="id">
/// The unique id of this node.
/// </param>
/// <param name="selection">
/// The selection that shall be resolved.
/// </param>
/// <exception cref="ArgumentNullException">
/// <paramref name="selection"/> is <c>null</c>.
/// </exception>
internal sealed class ResolveNode(int id, Selection selection) : QueryPlanNode(id)
{
    private readonly Dictionary<string, QueryPlanNode> _fetchNodes = new(StringComparer.Ordinal);
    private readonly Selection _selection = selection ?? throw new ArgumentNullException(nameof(selection));

    /// <summary>
    /// Gets the kind of this node.
    /// </summary>
    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.ResolveNode;

    /// <summary>
    /// Executes this resolver node.
    /// </summary>
    /// <param name="context">
    /// The execution context.
    /// </param>
    /// <param name="state">
    /// The execution state.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    protected override async Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        RequestState state,
        CancellationToken cancellationToken)
    {
        var variables = context.OperationContext.Variables;
        var coercedArguments = new Dictionary<string, ArgumentValue>();

        _selection.Arguments.CoerceArguments(variables, coercedArguments);

        var idArgument = coercedArguments["id"];

        if (idArgument.ValueLiteral is not StringValueNode formattedId)
        {
            context.Result.AddError(InvalidNodeFormat(_selection), _selection);
            return;
        }

        string typeName;

        try
        {
            typeName = context.ParseTypeNameFromId(formattedId.Value);
        }
        catch (IdSerializationException ex)
        {
            context.Result.AddError(InvalidNodeFormat(_selection, ex), _selection);
            return;
        }

        if(!_fetchNodes.TryGetValue(typeName, out var fetchNode))
        {
            context.Result.AddError(InvalidNodeFormat(_selection), _selection);
            return;
        }

        await fetchNode.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Registers an entity resolver.
    /// </summary>
    /// <param name="typeName">
    /// The name of the entity type for which the resolver shall be registered.
    /// </param>
    /// <param name="resolveEntity">
    /// The resolver that shall be registered.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The node is read-only.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="typeName"/> or <paramref name="resolveEntity"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A resolver for the specified <paramref name="typeName"/> is already registered.
    /// </exception>
    public void AddEntityResolver(string typeName, Resolve resolveEntity)
    {
        if(IsReadOnly)
        {
            throw ThrowHelper.Node_ReadOnly();
        }

        ArgumentNullException.ThrowIfNull(typeName);
        ArgumentNullException.ThrowIfNull(resolveEntity);

        if (_fetchNodes.ContainsKey(typeName))
        {
            throw new ArgumentException(
                ResolveNode_EntityResolver_Already_Registered,
                paramName: nameof(typeName));
        }

        _fetchNodes.Add(typeName, resolveEntity);
        base.AddNode(resolveEntity);
    }

    internal override void AddNode(QueryPlanNode node)
        => throw new NotSupportedException();

    protected override void FormatProperties(Utf8JsonWriter writer)
    {
        writer.WriteNumber(SelectionIdProp, _selection.Id);
        writer.WriteString(ResponseNameProp, _selection.ResponseName);
        base.FormatProperties(writer);
    }

    protected override void FormatNodesProperty(Utf8JsonWriter writer)
    {
        if (_fetchNodes.Count > 0)
        {
            writer.WritePropertyName(BranchesProp);
            writer.WriteStartArray();

            foreach (var (type, node) in _fetchNodes)
            {
                writer.WriteStartObject();
                writer.WriteString(TypeProp, type);
                writer.WritePropertyName(NodeProp);
                node.Format(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }
}
