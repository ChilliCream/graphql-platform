using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The resolver node is responsible for executing a GraphQL request on a subgraph.
/// This represents the base class for various resolver node implementations.
/// </summary>
internal abstract class ResolverNodeBase : QueryPlanNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="ResolverNodeBase"/>.
    /// </summary>
    /// <param name="id">
    /// The unique id of this node.
    /// </param>
    /// <param name="subgraphName">
    /// The name of the subgraph on which this request handler executes.
    /// </param>
    /// <param name="document">
    /// The GraphQL request document.
    /// </param>
    /// <param name="selectionSet">
    /// The selection set for which this request provides a patch.
    /// </param>
    /// <param name="requires">
    /// The variables that this request handler requires to create a request.
    /// </param>
    /// <param name="path">
    /// The path to the data that this request handler needs to extract.
    /// </param>
    /// <param name="forwardedVariables">
    /// The variables that this request handler forwards to the subgraph.
    /// </param>
    protected ResolverNodeBase(
        int id,
        string subgraphName,
        DocumentNode document,
        ISelectionSet selectionSet,
        IReadOnlyList<string> requires,
        IReadOnlyList<string> path,
        IReadOnlyList<string> forwardedVariables)
        : base(id)
    {
        SubgraphName = subgraphName;
        Document = document.ToString(false);
        SelectionSet = (SelectionSet)selectionSet;
        Requires = requires;
        Path = path;
        ForwardedVariables = forwardedVariables;
    }

    /// <summary>
    /// Gets the schema name on which this request handler executes.
    /// </summary>
    public string SubgraphName { get; }

    /// <summary>
    /// Gets the GraphQL request document.
    /// </summary>
    public string Document { get; }

    /// <summary>
    /// Gets the selection set for which this request provides a patch.
    /// </summary>
    public SelectionSet SelectionSet { get; }

    /// <summary>
    /// Gets the variables that this request handler requires to create a request.
    /// </summary>
    public IReadOnlyList<string> Requires { get; }

    /// <summary>
    /// Gets the variables that this request handler forwards to the subgraph.
    /// </summary>
    public IReadOnlyList<string> ForwardedVariables { get; }

    /// <summary>
    /// Gets the path to the data that this request handler needs to extract.
    /// </summary>
    public IReadOnlyList<string> Path { get; }

    /// <summary>
    /// Creates a GraphQL request with the specified variable values.
    /// </summary>
    /// <param name="variables">
    /// The variables that where available on the original request.
    /// </param>
    /// <param name="requirementValues">
    /// The variables values that where extracted from the parent request.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    protected SubgraphGraphQLRequest CreateRequest(
        IVariableValueCollection variables,
        IReadOnlyDictionary<string, IValueNode> requirementValues)
    {
        ObjectValueNode? vars = null;

        if (Requires.Count > 0 || ForwardedVariables.Count > 0)
        {
            var fields = new List<ObjectFieldNode>();

            foreach (var forwardedVariable in ForwardedVariables)
            {
                if (variables.TryGetVariable<IValueNode>(forwardedVariable, out var value) &&
                    value is not null)
                {
                    fields.Add(new ObjectFieldNode(forwardedVariable, value));
                }
            }

            foreach (var requirement in Requires)
            {
                if (requirementValues.TryGetValue(requirement, out var value))
                {
                    fields.Add(new ObjectFieldNode(requirement, value));
                }
                else
                {
                    throw ThrowHelper.Requirement_Is_Missing(requirement, nameof(requirementValues));
                }
            }

            vars ??= new ObjectValueNode(fields);
        }

        return new SubgraphGraphQLRequest(SubgraphName, Document, vars, null);
    }

    /// <summary>
    /// Unwraps the result from the GraphQL response that is needed by this query plan node.
    /// </summary>
    /// <param name="response">
    /// The GraphQL response.
    /// </param>
    /// <returns>
    /// The unwrapped result.
    /// </returns>
    protected JsonElement UnwrapResult(GraphQLResponse response)
    {
        if (Path.Count == 0)
        {
            return response.Data;
        }

        if (response.Data.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
        {
            var current = response.Data;

            for (var i = 0; i < Path.Count; i++)
            {
                if (current.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                {
                    return current;
                }

                current.TryGetProperty(Path[i], out var propertyValue);
                current = propertyValue;
            }

            return current;
        }

        return response.Data;
    }

    /// <summary>
    /// Formats the properties of this query plan node in order to create a JSON representation.
    /// </summary>
    /// <param name="writer">
    /// The writer that is used to write the JSON representation.
    /// </param>
    protected override void FormatProperties(Utf8JsonWriter writer)
    {
        writer.WriteString("subgraph", SubgraphName);
        writer.WriteString("document", Document);
        writer.WriteNumber("selectionSetId", SelectionSet.Id);

        if (Path.Count > 0)
        {
            writer.WritePropertyName("path");
            writer.WriteStartArray();

            foreach (var path in Path)
            {
                writer.WriteStringValue(path);
            }
            writer.WriteEndArray();
        }

        if (Requires.Count > 0)
        {
            writer.WritePropertyName("requires");
            writer.WriteStartArray();

            foreach (var requirement in Requires)
            {
                writer.WriteStartObject();
                writer.WriteString("variable", requirement);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        if (ForwardedVariables.Count > 0)
        {
            writer.WritePropertyName("forwardedVariables");
            writer.WriteStartArray();

            foreach (var variable in ForwardedVariables)
            {
                writer.WriteStartObject();
                writer.WriteString("variable", variable);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}
