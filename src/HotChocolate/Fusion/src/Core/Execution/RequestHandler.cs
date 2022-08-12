using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Language;
using GraphQLRequest = HotChocolate.Fusion.Clients.GraphQLRequest;

namespace HotChocolate.Fusion.Execution;

internal sealed class RequestHandler
{
    private readonly IReadOnlyList<string> _path;

    internal RequestHandler(
        string schemaName,
        DocumentNode document,
        ISelectionSet selectionSet,
        IReadOnlyList<RequiredState> requires,
        IReadOnlyList<string> path)
    {
        SchemaName = schemaName;
        Document = document;
        SelectionSet = selectionSet;
        Requires = requires;
        _path = path;
    }

    /// <summary>
    /// Gets the schema name on which this request handler executes.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    /// Gets the GraphQL request document.
    /// </summary>
    public DocumentNode Document { get; }

    /// <summary>
    /// Gets the selection set for which this request provides a patch.
    /// </summary>
    public ISelectionSet SelectionSet { get; }

    /// <summary>
    /// Gets the variables that this request handler requires to create a request.
    /// </summary>
    public IReadOnlyList<RequiredState> Requires { get; }

    public GraphQLRequest CreateRequest(IReadOnlyDictionary<string, IValueNode> variableValues)
    {
        ObjectValueNode? vars = null;

        if (Requires.Count > 0)
        {
            var fields = new List<ObjectFieldNode>();

            foreach (var required in Requires)
            {
                if (variableValues.TryGetValue(required.VariableName, out var value))
                {
                    fields.Add(new ObjectFieldNode(required.VariableName, value));
                }
                else if (!required.IsOptional)
                {
                    // TODO : error helper
                    throw new ArgumentException(
                        $"The variable value `{required.VariableName}` was not provided " +
                        "but is required.",
                        nameof(variableValues));
                }
            }

            vars ??= new ObjectValueNode(fields);
        }

        return new GraphQLRequest(SchemaName, Document, vars, null);
    }

    public JsonElement UnwrapResult(GraphQLResponse response)
    {
        if (_path.Count == 0)
        {
            return response.Data;
        }

        if (response.Data.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
        {
            var current = response.Data;

            for (var i = 0; i < _path.Count; i++)
            {
                if (current.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                {
                    return current;
                }

                current.TryGetProperty(_path[i], out var propertyValue);
                current = propertyValue;
            }

            return current;
        }

        return response.Data;
    }
}

internal readonly struct RequiredState
{
    public RequiredState(string variableName, ITypeNode type, bool isOptional)
    {
        VariableName = variableName;
        Type = type;
        IsOptional = isOptional;
    }

    public string VariableName { get; }

    public ITypeNode Type { get; }

    public bool IsOptional { get; }
}
