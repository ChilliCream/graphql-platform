using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Metadata.FusionDirectiveNames;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Metadata;

internal sealed class ServiceConfiguration
{
    private readonly string[] _bindings;
    private readonly Dictionary<string, IType> _types;
    private readonly Dictionary<(string, string), string> _typeNameLookup = new();

    public ServiceConfiguration(IEnumerable<string> bindings, IEnumerable<IType> types)
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

    public T GetType<T>(string schemaName, string typeName) where T : IType
    {
        if (!_typeNameLookup.TryGetValue((schemaName, typeName), out var temp))
        {
            temp = typeName;
        }

        if (_types.TryGetValue(temp, out var type) && type is T casted)
        {
            return casted;
        }

        throw new InvalidOperationException("Type not found.");
    }

    public T GetType<T>(TypeInfo typeInfo) where T : IType
    {
        throw new NotImplementedException();
    }

    public string GetTypeName(string schemaName, string typeName)
    {
        if (!_typeNameLookup.TryGetValue((schemaName, typeName), out var temp))
        {
            temp = typeName;
        }

        return temp;
    }

    public string GetTypeName(TypeInfo typeInfo)
    {
        throw new NotImplementedException();
    }

    public static ServiceConfiguration Load(string sourceText)
        => new SchemaReader().Read(sourceText);
}

internal sealed class SchemaReader
{
    private readonly HashSet<string> _assert = new();

    public ServiceConfiguration Read(string sourceText)
        => ReadServiceDefinition(Utf8GraphQLParser.Parse(sourceText));

    private ServiceConfiguration ReadServiceDefinition(DocumentNode documentNode)
    {
        var types = new List<IType>();
        IReadOnlyList<HttpClientConfig>? httpClientConfigs = null;

        foreach (var definition in documentNode.Definitions)
        {
            switch (definition)
            {
                case ObjectTypeDefinitionNode node:
                    types.Add(ReadObjectType(node));
                    break;

                case SchemaDefinitionNode node:
                    httpClientConfigs = ReadHttpClientConfigs(node.Directives);
                    break;
            }
        }

        if (httpClientConfigs is not { Count: > 0 })
        {
            // TODO : EXCEPTION
            throw new Exception("No clients configured");
        }

        if (types.Count == 0)
        {
            // TODO : EXCEPTION
            throw new Exception("No types");
        }


        return new ServiceConfiguration(
            httpClientConfigs.Select(t => t.SchemaName),
            types);
    }

    private ObjectType ReadObjectType(ObjectTypeDefinitionNode typeDef)
    {
        var variables = ReadFieldVariableDefinitions(typeDef.Directives);
        var resolvers = ReadFetchDefinitions(typeDef.Directives);
        var fields = ReadObjectFields(typeDef.Fields);
        return new ObjectType(typeDef.Name.Value, variables, resolvers, fields);
    }

    private ObjectFieldCollection ReadObjectFields(
        IReadOnlyList<FieldDefinitionNode> fieldDefinitionNodes)
    {
        var collection = new List<ObjectField>();

        foreach (var fieldDef in fieldDefinitionNodes)
        {
            var resolvers = ReadFetchDefinitions(fieldDef.Directives);
            var bindings = ReadMemberBindings(fieldDef.Directives, fieldDef, resolvers);
            var variables = ReadArgumentVariableDefinitions(fieldDef.Directives, fieldDef);
            var field = new ObjectField(fieldDef.Name.Value, bindings, variables, resolvers);
            collection.Add(field);
        }

        return new ObjectFieldCollection(collection);
    }

    private IReadOnlyList<HttpClientConfig> ReadHttpClientConfigs(
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        var configs = new List<HttpClientConfig>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(HttpDirective))
            {
                configs.Add(ReadHttpClientConfig(directiveNode));
            }
        }

        return configs;
    }

    private HttpClientConfig ReadHttpClientConfig(
        DirectiveNode directiveNode)
    {
        AssertName(directiveNode, HttpDirective);
        AssertArguments(directiveNode, NameArg, BaseAddressArg);

        string name = default!; ;
        string baseAddress = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case NameArg:
                    name = Expect<StringValueNode>(argument.Value).Value;
                    break;

                case BaseAddressArg:
                    baseAddress = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        return new HttpClientConfig(name, new Uri(baseAddress));
    }

    private VariableDefinitionCollection ReadFieldVariableDefinitions(
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        var definitions = new List<FieldVariableDefinition>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(VariableDirective))
            {
                definitions.Add(ReadFieldVariableDefinition(directiveNode));
            }
        }

        return new VariableDefinitionCollection(definitions);
    }

    private FieldVariableDefinition ReadFieldVariableDefinition(DirectiveNode directiveNode)
    {
        AssertName(directiveNode, VariableDirective);
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
            if (directiveNode.Name.Value.EqualsOrdinal(FetchDirective))
            {
                definitions.Add(ReadFetchDefinition(directiveNode));
            }
        }

        return new FetchDefinitionCollection(definitions);
    }

    private FetchDefinition ReadFetchDefinition(DirectiveNode directiveNode)
    {
        AssertName(directiveNode, FetchDirective);
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
                        return SyntaxVisitor.Break;
                    }

                    if (node is VariableNode v)
                    {
                        _assert.Add(v.Name.Value);
                    }

                    return SyntaxVisitor.Continue;
                },
                options: new() { VisitArguments = true })
            .Visit(select);

        return new FetchDefinition(
            schemaName,
            select,
            placeholder,
            _assert.Count == 0
                ? Array.Empty<string>()
                : _assert.ToArray());
    }

    private MemberBindingCollection ReadMemberBindings(
        IReadOnlyList<DirectiveNode> directiveNodes,
        FieldDefinitionNode annotatedField,
        FetchDefinitionCollection resolvers)
    {
        var definitions = new List<MemberBinding>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(BindDirective))
            {
                definitions.Add(ReadMemberBinding(directiveNode, annotatedField));
            }
        }

        if (resolvers.Count > 0)
        {
            _assert.Clear();

            foreach (var binding in definitions)
            {
                _assert.Add(binding.SchemaName);
            }

            foreach (var resolver in resolvers)
            {
                if (_assert.Add(resolver.SchemaName))
                {
                    definitions.Add(
                        new MemberBinding(resolver.SchemaName, annotatedField.Name.Value));
                }
            }
        }

        return new MemberBindingCollection(definitions);
    }

    private MemberBinding ReadMemberBinding(
        DirectiveNode directiveNode,
        FieldDefinitionNode annotatedField)
    {
        AssertName(directiveNode, BindDirective);
        AssertArguments(directiveNode, ToArg, AsArg);

        string? name = null;
        string schemaName = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case AsArg:
                    name = Expect<StringValueNode>(argument.Value).Value;
                    break;

                case ToArg:
                    schemaName = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        return new MemberBinding(schemaName, name ?? annotatedField.Name.Value);
    }

    private ArgumentVariableDefinitionCollection ReadArgumentVariableDefinitions(
        IReadOnlyList<DirectiveNode> directiveNodes,
        FieldDefinitionNode annotatedField)
    {
        var definitions = new List<ArgumentVariableDefinition>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(VariableDirective))
            {
                definitions.Add(ReadArgumentVariableDefinition(directiveNode, annotatedField));
            }
        }

        return new ArgumentVariableDefinitionCollection(definitions);
    }

    private ArgumentVariableDefinition ReadArgumentVariableDefinition(
        DirectiveNode directiveNode,
        FieldDefinitionNode annotatedField)
    {
        AssertName(directiveNode, VariableDirective);
        AssertArguments(directiveNode, NameArg, ArgumentArg);

        string name = default!;
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
    public const string VariableDirective = "variable";
    public const string FetchDirective = "fetch";
    public const string BindDirective = "bind";
    public const string HttpDirective = "httpClient";
    public const string NameArg = "name";
    public const string SelectArg = "select";
    public const string TypeArg = "type";
    public const string FromArg = "from";
    public const string ToArg = "to";
    public const string AsArg = "as";
    public const string ArgumentArg = "argument";
    public const string BaseAddressArg = "baseAddress";
}

public readonly struct TypeInfo
{
    public TypeInfo(string schemaName, string typeName)
    {
        SchemaName = schemaName;
        TypeName = typeName;
    }

    public string SchemaName { get; }

    public string TypeName { get; }
}
