using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Metadata.FusionDirectiveNames;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Metadata;

// TODO : Name .... RemoteService?
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

    public static Schema Load(string body) => throw new NotImplementedException();
}

internal sealed class SchemaReader
{
    private readonly HashSet<string> _assert = new();


    private ObjectType ReadObjectType(ObjectTypeDefinitionNode typeDef)
    {
        var variableDefinitions = ReadFieldVariableDefinitions(typeDef.Directives);


    }

    private ObjectFieldCollection ReadObjectFields(
        IReadOnlyList<FieldDefinitionNode> fieldDefinitionNodes)
    {
        foreach (var field in fieldDefinitionNodes)
        {
            var resolvers = ReadFetchDefinitions(field.Directives);

        }
    }

    private VariableDefinitionCollection ReadFieldVariableDefinitions(
        IReadOnlyList<DirectiveNode> directiveNodes)
    {

    }

    private FieldVariableDefinition ReadFieldVariableDefinition(DirectiveNode directiveNode)
    {
        AssertName(directiveNode, Variable);
        AssertArguments(directiveNode, NameArg, SelectArg, TypeArg, FromArg);

        string name = default!;
        FieldNode select = default!;
        ITypeNode type = default!;
        string schemaName = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case NameArg:
                    name = Expect<StringValueNode>(argument.Value).Value;
                    break;

                case SelectArg:
                    select = ParseField(Expect<StringValueNode>(argument.Value).Value);
                    break;

                case TypeArg:
                    type = ParseTypeReference(Expect<StringValueNode>(argument.Value).Value);
                    break;

                case FromArg:
                    schemaName = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        return new FieldVariableDefinition(name, schemaName, type, select);
    }

    private FetchDefinitionCollection ReadFetchDefinitions(
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        var definitions = new List<FetchDefinition>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(Fetch))
            {
                definitions.Add(ReadFetchDefinition(directiveNode));
            }
        }

        return new FetchDefinitionCollection(definitions);
    }

    private FetchDefinition ReadFetchDefinition(DirectiveNode directiveNode)
    {
        AssertName(directiveNode, Fetch);
        AssertArguments(directiveNode, SelectArg, FromArg);

        ISelectionNode select = default!;
        string schemaName = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case SelectArg:
                    select = ParseField(Expect<StringValueNode>(argument.Value).Value);
                    break;

                case FromArg:
                    schemaName = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        FragmentSpreadNode? placeholder = null;
        _assert.Clear();

        SyntaxVisitor
            .Create(
                enter: node =>
                {
                    if (node is FragmentSpreadNode p)
                    {
                        placeholder = p;
                    }
                    else if (node is VariableNode v)
                    {
                        _assert.Add(v.Name.Value);
                    }

                    return SyntaxVisitor.Continue;
                })
            .Visit(select);

        return new FetchDefinition(
            schemaName,
            select,
            placeholder,
            _assert.Count == 0
                ? Array.Empty<string>()
                : _assert.ToArray());
    }

    private MemberBinding ReadMemberBinding(DirectiveNode directiveNode)
    {
        AssertName(directiveNode, Bind);
        AssertArguments(directiveNode, ToArg, AsArg);

        string name = default!;
        string schemaName = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case NameArg:
                    name = Expect<StringValueNode>(argument.Value).Value;
                    break;

                case FromArg:
                    schemaName = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        return new MemberBinding(name, schemaName);
    }

    private ArgumentVariableDefinition ReadArgumentVariableDefinition(
        DirectiveNode directiveNode,
        FieldDefinitionNode annotatedField)
    {
        AssertName(directiveNode, Variable);
        AssertArguments(directiveNode, NameArg, ArgumentArg);

        string name = default!; ;
        string argumentName = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case NameArg:
                    name = Expect<StringValueNode>(argument.Value).Value;
                    break;

                case ArgumentArg:
                    argumentName = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        var arg = annotatedField.Arguments.Single(t => t.Name.Value.EqualsOrdinal(argumentName));

        return new ArgumentVariableDefinition(name, arg.Type, argumentName);
    }

    private static T Expect<T>(IValueNode valueNode) where T : IValueNode
    {
        if (valueNode is not T casted)
        {
            // TODO : EXCEPTION
            throw new InvalidOperationException("Invalid value");
        }

        return casted;
    }

    private void AssertName(DirectiveNode directive, string expectedName)
    {
        if (!directive.Name.Value.EqualsOrdinal(expectedName))
        {
            // TODO : EXCEPTION
            throw new InvalidOperationException("INVALID DIRECTIVE NAME");
        }
    }

    private void AssertArguments(DirectiveNode directive, params string[] expectedArguments)
    {
        if (directive.Arguments.Count < 0)
        {
            // TODO : EXCEPTION
            throw new InvalidOperationException("INVALID ARGS");
        }

        _assert.Clear();

        foreach (var argument in directive.Arguments)
        {
            _assert.Add(argument.Name.Value);
        }

        _assert.ExceptWith(expectedArguments);

        if (_assert.Count > 0)
        {
            // TODO : EXCEPTION
            throw new InvalidOperationException("INVALID ARGS");
        }
    }
}

internal static class FusionDirectiveNames
{
    public const string Variable = "variable";
    public const string Fetch = "fetch";
    public const string Bind = "bind";
    public const string NameArg = "name";
    public const string SelectArg = "select";
    public const string TypeArg = "type";
    public const string FromArg = "from";
    public const string ToArg = "to";
    public const string AsArg = "as";
    public const string ArgumentArg = "argument";
}
