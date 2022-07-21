using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Resolvers;

namespace HotChocolate.Fusion;

public class QueryPlanBuilder
{
    private readonly Schema _schema;
    private readonly IOperation _operation;

    public QueryPlanBuilder(Schema schema, IOperation operation)
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

public interface IType
{
    string Name { get; }
}

public sealed class Schema
{
    private readonly string[] _bindings;
    private readonly Dictionary<string, IType> _types;

    public Schema(IEnumerable<string> bindings, IEnumerable<IType> types)
    {
        _bindings = bindings.ToArray();
        _types = types.ToDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<string> Bindings => _bindings;

    public T GetType<T>(string typeName) where T : IType
    {
        if (_types.TryGetValue(typeName, out var type) && type is T casted)
        {
            return casted;
        }

        throw new InvalidOperationException("Type not found.");
    }
}

public sealed class ObjectType : IType
{
    public ObjectType(
        string name,
        IEnumerable<MemberBinding> bindings,
        IEnumerable<FetchDefinition> resolvers,
        IEnumerable<ObjectField> fields)
    {
        Name = name;
        Bindings = new MemberBindingCollection(bindings);
        Resolvers = new FetchDefinitionCollection(resolvers);
        Fields = new ObjectFieldCollection(fields);
    }

    public string Name { get; }

    public MemberBindingCollection Bindings { get; }

    public FetchDefinitionCollection Resolvers { get; }

    public VariableDefinitionCollection Variables => throw new NotImplementedException();

    public ObjectFieldCollection Fields { get; }
}

public sealed class ObjectField
{
    public ObjectField(
        string name,
        IEnumerable<MemberBinding> bindings,
        IEnumerable<FetchDefinition> resolvers)
    {
        Name = name;
        Bindings = new MemberBindingCollection(bindings);
        Resolvers = new FetchDefinitionCollection(resolvers);
    }

    public string Name { get; }

    public MemberBindingCollection Bindings { get; }

    public ArgumentVariableDefinitionCollection Variables => throw new NotImplementedException();

    public FetchDefinitionCollection Resolvers { get; }
}

public sealed class ObjectFieldCollection : IEnumerable<ObjectField>
{
    private readonly Dictionary<string, ObjectField> _fields;

    public ObjectFieldCollection(IEnumerable<ObjectField> fields)
    {
        _fields = fields.ToDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public int Count => _fields.Count;

    public ObjectField this[string fieldName] => _fields[fieldName];

    public bool TryGetValue(string fieldName, [NotNullWhen(true)] out ObjectField? value)
        => _fields.TryGetValue(fieldName, out value);

    public IEnumerator<ObjectField> GetEnumerator() => _fields.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class MemberBindingCollection : IEnumerable<MemberBinding>
{
    private readonly Dictionary<string, MemberBinding> _bindings;

    public MemberBindingCollection(IEnumerable<MemberBinding> bindings)
    {
        _bindings = bindings.ToDictionary(t => t.SchemaName, StringComparer.Ordinal);
    }

    public int Count => _bindings.Count;

    // public MemberBinding this[string schemaName] => throw new NotImplementedException();

    public bool TryGetValue(string schemaName, [NotNullWhen(true)] out MemberBinding? value)
        => _bindings.TryGetValue(schemaName, out value);

    public bool ContainsSchema(string schemaName) => _bindings.ContainsKey(schemaName);

    public IEnumerator<MemberBinding> GetEnumerator() => _bindings.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class FetchDefinitionCollection : IEnumerable<FetchDefinition>
{
    private readonly Dictionary<string, FetchDefinition[]> _fetchDefinitions;

    public FetchDefinitionCollection(IEnumerable<FetchDefinition> fetchDefinitions)
    {
        _fetchDefinitions = fetchDefinitions
            .GroupBy(t => t.SchemaName)
            .ToDictionary(t => t.Key, t => t.ToArray(), StringComparer.Ordinal);
    }

    public int Count => _fetchDefinitions.Count;

    // public IReadOnlyList<FetchDefinition> this[string schemaName] => throw new NotImplementedException();

    public bool TryGetValue(
        string schemaName,
        [NotNullWhen(true)] out IReadOnlyList<FetchDefinition>? values)
    {
        if (_fetchDefinitions.TryGetValue(schemaName, out var temp))
        {
            values = temp;
            return true;
        }

        values = null;
        return false;
    }

    public bool ContainsResolvers(string schemaName) => _fetchDefinitions.ContainsKey(schemaName);

    public IEnumerator<FetchDefinition> GetEnumerator()
        => _fetchDefinitions.Values.SelectMany(t => t).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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

public class ArgumentVariableDefinitionCollection : IEnumerable<ArgumentVariableDefinition>
{
    public int Count { get; }

    public ArgumentVariableDefinition this[string variableName]
        => throw new NotImplementedException();

    public bool TryGetValue(string variableName, out ArgumentVariableDefinition value)
    {
        throw new NotImplementedException();
    }

    public bool ContainsVariable(string variableName) => throw new NotImplementedException();

    public IEnumerator<ArgumentVariableDefinition> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// The type system member binding information.
/// </summary>
public class MemberBinding
{
    /// <summary>
    /// Initializes a new instance of <see cref="MemberBinding"/>.
    /// </summary>
    /// <param name="schemaName">
    /// The schema to which the type system member is bound to.
    /// </param>
    /// <param name="name">
    /// The name which the type system member has in the <see cref="SchemaName"/>.
    /// </param>
    public MemberBinding(string schemaName, string name)
    {
        SchemaName = schemaName;
        Name = name;
    }

    /// <summary>
    /// Gets the schema to which the type system member is bound to.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    /// Gets the name which the type system member has in the <see cref="SchemaName"/>.
    /// </summary>
    public string Name { get; }
}

public class FetchDefinition
{
    public FetchDefinition(
        string schemaName,
        ISelectionNode select,
        FragmentSpreadNode? placeholder,
        IReadOnlyList<string> requires)
    {
        SchemaName = schemaName;
        Select = select;
        Placeholder = placeholder;
        Requires = requires;
    }

    /// <summary>
    /// Gets the schema to which the type system member is bound to.
    /// </summary>
    public string SchemaName { get; }

    public ISelectionNode Select { get; }

    public FragmentSpreadNode? Placeholder { get; }

    public IReadOnlyList<string> Requires { get; }

    public ISelectionNode CreateSelection(
        IReadOnlyDictionary<string, string> variables,
        SelectionSetNode? selectionSet)
    {

    }

    private class FetchRewriter : SyntaxRewriter<FetchRewriterContext>
    {
        

        protected override FragmentSpreadNode? RewriteFragmentSpread(
            FragmentSpreadNode node,
            FetchRewriterContext context)
        {
            if (ReferenceEquals(context.Placeholder, node))
            {

            }

            return base.RewriteFragmentSpread(node, context);
        }
    }

    private sealed class FetchRewriterContext : ISyntaxVisitorContext
    {
        public FragmentSpreadNode? Placeholder { get; }

        public IReadOnlyDictionary<string, string> Variables { get; }

        public SelectionSetNode? SelectionSet { get; }
    }
}

public sealed class FieldVariableDefinition : IVariableDefinition
{
    public FieldVariableDefinition(string name, IType type, SelectionSetNode select)
    {
        Name = name;
        Type = type;
        Select = select;
    }

    public string Name { get; }

    public IType Type { get; }

    public SelectionSetNode Select { get; }
}

public sealed class ArgumentVariableDefinition : IVariableDefinition
{
    public ArgumentVariableDefinition(string name, IType type, string argumentName)
    {
        Name = name;
        Type = type;
        ArgumentName = argumentName;
    }

    public string Name { get; }

    public IType Type { get; }

    public string ArgumentName { get; }
}

public interface IVariableDefinition
{
    string Name { get; }

    IType Type { get; }
}
