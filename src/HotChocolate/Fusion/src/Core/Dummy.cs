using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion;

public class QueryPlanBuilder
{
    private readonly Types.Schema _schema;
    private readonly IOperation _operation;

    public QueryPlanBuilder(Types.Schema schema, IOperation operation)
    {
        _schema = schema;
        _operation = operation;
    }

    public void CollectSelectionsBySchema1(
        IReadOnlyList<ISelection> selections,
        ObjectType typeContext,
        ExecutionNode parent)
    {
        CollectSelectionsBySchema2(new Context(), selections, typeContext, parent);
    }

    private void CollectSelectionsBySchema2(
        Context context,
        IReadOnlyList<ISelection> selections,
        ObjectType typeContext,
        ExecutionNode parent)
    {
        var schemaName = ResolveBestMatchingSchema(_operation, selections, typeContext);
        var syntaxList = new List<ISelectionNode>();

        foreach (var selection in selections)
        {
            var field = typeContext.Fields[selection.Field.Name];

            if (field.Bindings.TryGetValue(schemaName, out var binding))
            {
                context.Variables.Clear();

                foreach (var variable in field.Variables)
                {
                    context.Variables.Add(variable.Name, variable);
                }

                if (!TryGetResolver(field, schemaName, context.Variables, out var resolver))
                {
                    // todo : error message and type
                    throw new InvalidOperationException(
                        "There must be a field fetch definition valid in this context!");
                }

                syntaxList.Add(CreateSelectionSyntax(context, selection, binding, resolver));
            }
        }

        var selectionSetSyntax = new SelectionSetNode(syntaxList);
        var operationSyntax = new OperationDefinitionNode(
            null,
            null,
            OperationType.Query,
            Array.Empty<VariableDefinitionNode>(),
            Array.Empty<DirectiveNode>(),
            selectionSetSyntax);
        var document = new DocumentNode(new[] { operationSyntax });
        var requestHandler = new RequestHandler(document);
        var requestNode = new RequestNode(requestHandler);
        parent.AppendNode(requestNode);
    }

    private ISelectionNode CreateSelectionSyntax(
        Context context,
        ISelection selection,
        MemberBinding binding,
        FetchDefinition? resolver)
    {
        SelectionSetNode? selectionSetSyntax = null;

        if (selection.SelectionSet is not null)
        {
            selectionSetSyntax = CreateSelectionSetSyntax(
                context,
                selection,
                binding.SchemaName);
        }

        if (resolver is null)
        {
            var alias = !selection.ResponseName.Equals(binding.Name)
                ? new NameNode(selection.ResponseName)
                : null;

            return new FieldNode(
                null,
                new(binding.Name),
                alias,
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                selectionSetSyntax);
        }

        return resolver.CreateSelection(context.VariableMapping, selectionSetSyntax);
    }

    private SelectionSetNode CreateSelectionSetSyntax(
        Context context,
        ISelection parentSelection,
        string schemaName)
    {
        var syntaxList = new List<ISelectionNode>();
        var possibleTypes = _operation.GetPossibleTypes(parentSelection);

        foreach (var possibleType in possibleTypes)
        {
            var typeContext = _schema.GetType<ObjectType>(possibleType.Name);
            var selectionSet = _operation.GetSelectionSet(parentSelection, possibleType);

            foreach (var selection in selectionSet.Selections)
            {
                var field = typeContext.Fields[selection.Field.Name];

                if (field.Bindings.TryGetValue(schemaName, out var binding))
                {
                    FetchDefinition? resolver = null;

                    if (field.Resolvers.ContainsResolvers(schemaName) &&
                        !TryGetResolver(field, schemaName, context.Variables, out resolver))
                    {
                        // todo : error message and type
                        throw new InvalidOperationException(
                            "There must be a field fetch definition valid in this context!");
                    }

                    syntaxList.Add(CreateSelectionSyntax(context, selection, binding, resolver));
                }
            }
        }

        return new SelectionSetNode(syntaxList);
    }


    private string ResolveBestMatchingSchema(
        IOperation operation,
        IReadOnlyList<ISelection> selections,
        ObjectType typeContext)
    {
        var bestScore = 0;
        var bestSchema = _schema.Bindings[0];

        foreach (var schemaName in _schema.Bindings)
        {
            var score = CalculateSchemaScore(operation, selections, typeContext, schemaName);

            if (score > bestScore)
            {
                bestScore = score;
                bestSchema = schemaName;
            }
        }

        return bestSchema;
    }

    private int CalculateSchemaScore(
        IOperation operation,
        IReadOnlyList<ISelection> selections,
        ObjectType typeContext,
        string schemaName)
    {
        var score = 0;

        foreach (var selection in selections)
        {
            if (typeContext.Fields[selection.Field.Name].Bindings.ContainsSchema(schemaName))
            {
                score++;

                if (selection.SelectionSet is not null)
                {
                    foreach (var possibleType in operation.GetPossibleTypes(selection))
                    {
                        var type = _schema.GetType<ObjectType>(possibleType.Name);
                        var selectionSet = operation.GetSelectionSet(selection, possibleType);
                        score += CalculateSchemaScore(
                            operation,
                            selectionSet.Selections,
                            type,
                            schemaName);
                    }
                }
            }
        }

        return score;
    }

    private bool TryGetResolver(
        ObjectField field,
        string schemaName,
        Dictionary<string, IVariableDefinition> variables,
        [NotNullWhen(true)] out FetchDefinition? resolver)
    {
        if (field.Resolvers.TryGetValue(schemaName, out var resolvers))
        {
            foreach (var current in resolvers)
            {
                var canBeUsed = true;

                foreach (var requirement in current.Requires)
                {
                    if (!variables.ContainsKey(requirement))
                    {
                        canBeUsed = false;
                        break;
                    }
                }

                if (canBeUsed)
                {
                    resolver = current;
                    return true;
                }
            }
        }

        resolver = null;
        return false;
    }


    private class Context
    {
        public Dictionary<string, IVariableDefinition> Variables { get; } = new();

        public Dictionary<string, string> VariableMapping { get; } = new();
    }
}

public class QueryPlan : ExecutionNode { }

public sealed class RequestNode : ExecutionNode
{
    public RequestNode(RequestHandler handler)
    {
        Handler = handler;
    }

    public RequestHandler Handler { get; }
}

public abstract class ExecutionNode
{
    private readonly List<ExecutionNode> _nodes = new();
    private bool _isReadOnly = false;

    public IReadOnlyList<ExecutionNode> Nodes => _nodes;

    internal void AppendNode(ExecutionNode node)
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("The execution node is read-only.");
        }

        _nodes.Add(node);
    }

    internal void Seal()
    {
        if (!_isReadOnly)
        {
            _isReadOnly = true;

            foreach (var node in _nodes)
            {
                node.Seal();
            }
        }
    }
}

public class RequestHandler
{
    public RequestHandler(DocumentNode document)
    {
        Document = document;
    }

    public IReadOnlyList<string> Requires { get; }

    public IReadOnlyList<string> Exports { get; }

    public DocumentNode Document { get; }

    public Request CreateRequest(IReadOnlyList<IValueNode>? variables)
        => throw new NotImplementedException();

    public IReadOnlyList<IValueNode> ExtractExports(JsonElement response)
        => throw new NotImplementedException();

    public void ExtractResult(JsonElement response, ObjectResult parent)
        => throw new NotImplementedException();
}

public readonly struct Request
{
    public DocumentNode Document { get; }

    public ObjectValueNode? VariableValues { get; }

    public ObjectValueNode? Extensions { get; }
}

public interface IType // should be called named type
{
    string Name { get; }
}

public class VariableDefinitionCollection
{
    public int Count { get; }

    public IReadOnlyList<IVariableDefinition> this[string variableName]
        => throw new NotImplementedException();

    public bool TryGetValue(string variableName, out IReadOnlyList<IVariableDefinition> value)
    {
        throw new NotImplementedException();
    }

    public bool ContainsVariable(string variableName) => throw new NotImplementedException();
}

public sealed class FieldVariableDefinition : IVariableDefinition
{
    public FieldVariableDefinition(string name, ITypeNode type, SelectionSetNode select)
    {
        Name = name;
        Type = type;
        Select = select;
    }

    public string Name { get; }

    public ITypeNode Type { get; }

    public SelectionSetNode Select { get; }
}
