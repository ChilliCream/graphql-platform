using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class RequestHandler
{
    internal RequestHandler(
        string schemaName,
        DocumentNode document,
        IReadOnlyList<RequiredState> requires)
    {
        SchemaName = schemaName;
        Document = document;
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

    public Request CreateRequest(IReadOnlyList<Argument> argumentValues)
        => throw new NotImplementedException();
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
