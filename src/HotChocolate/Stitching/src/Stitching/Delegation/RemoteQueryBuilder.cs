using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Delegation;

public class RemoteQueryBuilder
{
    private static readonly CollectUsedVariableVisitor _usedVariables = new();
    private readonly List<FieldNode> _additionalFields = new();
    private readonly List<VariableDefinitionNode> _variables = new();
    private readonly List<FragmentDefinitionNode> _fragments = new();
    private NameNode _operationName = new("fetch");
    private OperationType _operation = OperationType.Query;
    private IImmutableStack<SelectionPathComponent> _path =
        ImmutableStack<SelectionPathComponent>.Empty;
    private FieldNode? _requestField;

    public RemoteQueryBuilder SetOperation(
        NameNode? name,
        OperationType operation)
    {
        if (name != null)
        {
            _operationName = name;
        }

        _operation = operation;
        return this;
    }

    public RemoteQueryBuilder SetSelectionPath(
        IImmutableStack<SelectionPathComponent> selectionPath)
    {
        _path = selectionPath ?? throw new ArgumentNullException(nameof(selectionPath));
        return this;
    }

    public RemoteQueryBuilder SetRequestField(FieldNode field)
    {
        _requestField = field ?? throw new ArgumentNullException(nameof(field));
        return this;
    }

    public RemoteQueryBuilder AddAdditionalField(
        FieldNode field)
    {
        if (field == null)
        {
            throw new ArgumentNullException(nameof(field));
        }
        _additionalFields.Add(field);
        return this;
    }

    public RemoteQueryBuilder AddVariable(
        string name, ITypeNode type) =>
        AddVariable(name, type, null);

    public RemoteQueryBuilder AddVariable(
        string name,
        ITypeNode type,
        IValueNode? defaultValue)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        name.EnsureGraphQLName();

        AddVariable(new VariableDefinitionNode
        (
            null,
            new VariableNode(new NameNode(name)),
            type,
            defaultValue,
            Array.Empty<DirectiveNode>()
        ));

        return this;
    }

    public RemoteQueryBuilder AddVariable(
        VariableDefinitionNode variable)
    {
        if (variable == null)
        {
            throw new ArgumentNullException(nameof(variable));
        }

        _variables.Add(variable);

        return this;
    }

    public RemoteQueryBuilder AddVariables(
        IEnumerable<VariableDefinitionNode> variables)
    {
        if (variables == null)
        {
            throw new ArgumentNullException(nameof(variables));
        }

        _variables.AddRange(variables);

        return this;
    }

    public RemoteQueryBuilder AddFragmentDefinitions(
        IEnumerable<FragmentDefinitionNode> fragments)
    {
        if (fragments == null)
        {
            throw new ArgumentNullException(nameof(fragments));
        }

        _fragments.AddRange(fragments);

        return this;
    }

    public DocumentNode Build(
        string targetSchema,
        IReadOnlyDictionary<(string Type, string Schema), string> nameLookup)
    {
        if (_requestField == null || _path == null)
        {
            throw new InvalidOperationException();
        }

        var requestField = _requestField;

        if (_additionalFields.Count == 0)
        {
            return CreateDelegationQuery(
                targetSchema,
                nameLookup,
                _operation,
                _path,
                new[] { requestField });
        }

        var fields = new List<FieldNode> { _requestField };
        fields.AddRange(_additionalFields);

        return CreateDelegationQuery(
            targetSchema,
            nameLookup,
            _operation,
            _path,
            fields);
    }

    private DocumentNode CreateDelegationQuery(
        string targetSchema,
        IReadOnlyDictionary<(string Type, string Schema), string> nameLookup,
        OperationType operation,
        IImmutableStack<SelectionPathComponent> path,
        IEnumerable<FieldNode> requestedFields)
    {
        var usedVariables = new HashSet<string>();
        var fields = new List<FieldNode>();

        foreach (var requestedField in requestedFields)
        {
            var currentPath = path;

            if (currentPath.IsEmpty)
            {
                currentPath = currentPath.Push(
                    new SelectionPathComponent(
                        requestedField.Name,
                        Array.Empty<ArgumentNode>()));
            }

            var current = CreateRequestedField(requestedField, ref currentPath);

            while (!currentPath.IsEmpty)
            {
                currentPath = currentPath.Pop(out var component);
                current = CreateSelection(current, component);
            }

            _usedVariables.CollectUsedVariables(current, usedVariables);
            _usedVariables.CollectUsedVariables(_fragments, usedVariables);

            fields.Add(current);
        }

        var variables = _variables
            .Where(t => usedVariables.Contains(t.Variable.Name.Value))
            .ToList();

        for (var i = 0; i < variables.Count; i++)
        {
            var variable = variables[i];
            var typeName = variable.Type.NamedType().Name.Value;

            if (nameLookup.TryGetValue((typeName, targetSchema), out var targetName))
            {
                variable = variable.WithType(RewriteType(variable.Type, targetName));
                variables[i] = variable;
            }
        }

        var operationDefinition =
            CreateOperation(_operationName, operation, fields, variables);

        var definitions = new List<IDefinitionNode> { operationDefinition };
        definitions.AddRange(_fragments);

        return new DocumentNode(null, definitions);
    }

    private static ITypeNode RewriteType(ITypeNode type, string typeName)
    {
        if (type is NonNullTypeNode nonNullType)
        {
            return new NonNullTypeNode(
                (INullableTypeNode)RewriteType(nonNullType.Type, typeName));
        }

        if (type is ListTypeNode listTypeNode)
        {
            return new ListTypeNode(RewriteType(listTypeNode.Type, typeName));
        }

        return new NamedTypeNode(typeName);
    }

    private static FieldNode CreateRequestedField(
        FieldNode requestedField,
        ref IImmutableStack<SelectionPathComponent> path)
    {
        path = path.Pop(out var component);

        var responseName = requestedField.Alias == null
            ? requestedField.Name.Value
            : requestedField.Alias.Value;

        var alias = component.Name.Value.EqualsOrdinal(responseName)
            ? null
            : new NameNode(responseName);

        var arguments =
            component.Arguments.Count == 0
                ? requestedField.Arguments
                : RewriteVariableNames(component.Arguments).ToList();

        return new FieldNode
        (
            null,
            component.Name,
            alias,
            null,
            requestedField.Directives,
            arguments,
            requestedField.SelectionSet
        );
    }

    private static FieldNode CreateSelection(
        FieldNode previous,
        SelectionPathComponent next)
    {
        var selectionSet = new SelectionSetNode(
            null,
            new List<ISelectionNode> { previous });

        return CreateSelection(selectionSet, next, null);
    }

    private static FieldNode CreateSelection(
        SelectionSetNode selectionSet,
        SelectionPathComponent next,
        string? alias)
    {
        var aliasNode = string.IsNullOrEmpty(alias)
            ? null : new NameNode(alias);

        return new FieldNode
        (
            null,
            next.Name,
            aliasNode,
            null,
            Array.Empty<DirectiveNode>(),
            RewriteVariableNames(next.Arguments).ToList(),
            selectionSet
        );
    }

    private static OperationDefinitionNode CreateOperation(
        NameNode name,
        OperationType operation,
        IReadOnlyList<FieldNode> fields,
        IReadOnlyList<VariableDefinitionNode> variables)
    {
        return new OperationDefinitionNode(
            null,
            name,
            operation,
            variables,
            Array.Empty<DirectiveNode>(),
            new SelectionSetNode(null, fields));
    }

    private static IEnumerable<ArgumentNode> RewriteVariableNames(
        IEnumerable<ArgumentNode> arguments)
    {
        foreach (var argument in arguments)
        {
            if (argument.Value is ScopedVariableNode v)
            {
                yield return argument.WithValue(v.ToVariableNode());
            }
            else
            {
                yield return argument;
            }
        }
    }

    public static RemoteQueryBuilder New() => new RemoteQueryBuilder();
}
