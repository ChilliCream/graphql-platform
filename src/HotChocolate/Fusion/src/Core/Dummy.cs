using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion;

public class QueryPlanBuilder
{
    private readonly Schema _schema;


    public QueryPlanBuilder(Schema schema)
    {
        _schema = schema;
    }

    public void CollectSelectionsBySchema1(
        IOperation operation,
        ISelectionSet selectionSet,
        ObjectType typeContext)
    {
        var selections = new List<ISelection>(selectionSet.Selections);
        var schemaName = ResolveBestMatchingSchema(operation, selections, typeContext);
        var selectionSyntaxList = new List<ISelectionNode>();

        foreach (var selection in selections)
        {
            var field = typeContext.Fields[selection.Field.Name];

            if (field.Bindings.TryGetValue(schemaName, out var binding))
            {
                selectionSyntaxList.Add(CreateSelectionSyntax(selection, field, binding, null));
            }
        }


    }

    private ISelectionNode CreateSelectionSyntax(
        ISelection selection,
        ObjectField field,
        MemberBinding binding,
        FetchDefinition? resolver)
    {
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
                null);
        }

        throw new NotImplementedException();
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

    private void CreateRequest(
        IOperation operation,
        IReadOnlyList<ISelection> selections,
        ObjectType typeContext,
        string schemaName)
    {

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


}

public class QueryPlan : ExecutionNode
{
}

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
    public ObjectType(string name,
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

    // public VariableDefinitionCollection Variables { get; }

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

    public bool TryGetValue(string schemaName, out IReadOnlyList<FetchDefinition>? values)
    {
        if (_fetchDefinitions.TryGetValue(schemaName, out var temp))
        {
            values = temp;
            return true;
        }

        values = null;
        return false;
    }

    public bool ContainsSchema(string schemaName) => _fetchDefinitions.ContainsKey(schemaName);

    public IEnumerator<FetchDefinition> GetEnumerator()
        => _fetchDefinitions.Values.SelectMany(t => t).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class VariableDefinitionCollection
{
    public int Count { get; }

    public VariableDefinition this[string variableName] => throw new NotImplementedException();

    public bool TryGetValue(string variableName, out VariableDefinition value)
    {
        throw new NotImplementedException();
    }

    public bool ContainsSchema(string variableName) => throw new NotImplementedException();
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
        SelectionSetNode select,
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

    public SelectionSetNode Select { get; }

    public FragmentSpreadNode? Placeholder { get; }

    public IReadOnlyList<string> Requires { get; }
}

public class VariableDefinition
{
    public VariableDefinition(string name, IType type, SelectionSetNode select)
    {
        Name = name;
        Type = type;
        Select = select;
    }

    public string Name { get; }

    public IType Type { get; }

    public SelectionSetNode Select { get; }
}
