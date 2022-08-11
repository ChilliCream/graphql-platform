using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class RequestHandler
{
    internal RequestHandler(
        string schemaName,
        DocumentNode document,
        ISelectionSet selectionSet,
        IReadOnlyList<RequiredState> requires)
    {
        SchemaName = schemaName;
        Document = document;
        SelectionSet = selectionSet;
        Requires = requires;
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

    public Request CreateRequest(IReadOnlyDictionary<string, IValueNode> variableValues)
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

        return new Request(SchemaName, Document, vars, null);
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
