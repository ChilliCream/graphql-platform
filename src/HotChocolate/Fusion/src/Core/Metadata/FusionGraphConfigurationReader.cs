using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;
using static HotChocolate.Fusion.ThrowHelper;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Metadata;

internal sealed class FusionGraphConfigurationReader
{
    private readonly Dictionary<string, ITypeNode> _emptyArgumentDefs = new();
    private readonly HashSet<string> _assert = new();
    private readonly HashSet<string> _subgraphNames = new();

    public FusionGraphConfiguration Read(string sourceText)
        => Read(Utf8GraphQLParser.Parse(sourceText));

    public FusionGraphConfiguration Read(DocumentNode document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return ReadServiceDefinition(document);
    }

    private FusionGraphConfiguration ReadServiceDefinition(DocumentNode document)
    {
        var schemaDef = document.Definitions.OfType<SchemaDefinitionNode>().FirstOrDefault();

        if (schemaDef is null)
        {
            throw ServiceConfDocumentMustContainSchemaDef();
        }

        var types = new List<IType>();
        var typeNames = FusionTypeNames.From(document);
        var typeNameBindings = new Dictionary<string, MemberBinding>();
        var httpClientConfigs = ReadHttpClientConfigs(typeNames, schemaDef.Directives);
        var webSocketClientConfigs = ReadWebSocketClientConfigs(typeNames, schemaDef.Directives);
        var typeNameField = CreateTypeNameField(typeNameBindings);

        foreach (var definition in document.Definitions)
        {
            switch (definition)
            {
                case ObjectTypeDefinitionNode node:
                    types.Add(ReadObjectType(typeNames, node, typeNameField));
                    break;
            }
        }

        foreach (var subgraphName in _subgraphNames)
        {
            typeNameBindings.Add(subgraphName, new MemberBinding(subgraphName, typeNameField.Name));
        }

        if (httpClientConfigs is not { Count: > 0 })
        {
            throw ServiceConfNoClientsSpecified();
        }

        if (types.Count == 0)
        {
            throw ServiceConfNoTypesSpecified();
        }

        return new FusionGraphConfiguration(
            types,
            _subgraphNames.ToArray(),
            httpClientConfigs,
            webSocketClientConfigs);
    }

    private ObjectType ReadObjectType(
        FusionTypeNames typeNames,
        ObjectTypeDefinitionNode typeDef,
        ObjectField typeNameField)
    {
        var bindings = ReadMemberBindings(typeNames, typeDef.Directives, typeDef);
        var variables = ReadObjectVariableDefinitions(typeNames, typeDef.Directives);
        var resolvers = ReadResolverDefinitions(typeNames, typeDef.Directives);
        var fields = ReadObjectFields(typeNames, typeDef.Fields, typeNameField);
        return new ObjectType(typeDef.Name.Value, bindings, variables, resolvers, fields);
    }

    private ObjectFieldCollection ReadObjectFields(
        FusionTypeNames typeNames,
        IReadOnlyList<FieldDefinitionNode> fieldDefinitionNodes,
        ObjectField typeNameField)
    {
        var collection = new List<ObjectField>();

        foreach (var fieldDef in fieldDefinitionNodes)
        {
            var resolvers = ReadResolverDefinitions(typeNames, fieldDef.Directives);
            var bindings = ReadMemberBindings(typeNames, fieldDef.Directives, fieldDef, resolvers);
            var variables = ReadFieldVariableDefinitions(typeNames, fieldDef.Directives);
            var flags = ReadFlags(typeNames, fieldDef.Directives);
            var field = new ObjectField(fieldDef.Name.Value, flags, bindings, variables, resolvers);
            collection.Add(field);
        }

        collection.Add(typeNameField);

        return new ObjectFieldCollection(collection);
    }

    private static ObjectField CreateTypeNameField(
        Dictionary<string, MemberBinding> bindings)
    {
        return new ObjectField(
                IntrospectionFields.TypeName,
                ObjectFieldFlags.TypeName,
                new MemberBindingCollection(bindings),
                FieldVariableDefinitionCollection.Empty,
                ResolverDefinitionCollection.Empty);
    }

    private IReadOnlyList<HttpClientConfiguration> ReadHttpClientConfigs(
        FusionTypeNames typeNames,
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        var configs = new List<HttpClientConfiguration>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(typeNames.HttpDirective))
            {
                configs.Add(ReadHttpClientConfig(typeNames, directiveNode));
            }
        }

        return configs;
    }

    private HttpClientConfiguration ReadHttpClientConfig(
        FusionTypeNames typeNames,
        DirectiveNode directiveNode)
    {
        AssertName(directiveNode, typeNames.HttpDirective);
        AssertArguments(directiveNode, OptionalArgs, SubgraphArg, BaseAddressArg);

        string name = default!;
        string subgraph = default!;
        string baseAddress = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case ClientNameArg:
                    name = Expect<StringValueNode>(argument.Value).Value;
                    break;

                case SubgraphArg:
                    subgraph = Expect<StringValueNode>(argument.Value).Value;
                    break;

                case BaseAddressArg:
                    baseAddress = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        if (string.IsNullOrEmpty(name))
        {
            name = subgraph;
        }

        return new HttpClientConfiguration(name, subgraph, new Uri(baseAddress));

        static void OptionalArgs(HashSet<string> assert)
        {
            assert.Remove(ClientNameArg);
        }
    }

    private IReadOnlyList<WebSocketClientConfiguration> ReadWebSocketClientConfigs(
        FusionTypeNames typeNames,
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        var configs = new List<WebSocketClientConfiguration>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(typeNames.WebSocketDirective))
            {
                configs.Add(ReadWebSocketClientConfig(typeNames, directiveNode));
            }
        }

        return configs;
    }

    private WebSocketClientConfiguration ReadWebSocketClientConfig(
        FusionTypeNames typeNames,
        DirectiveNode directiveNode)
    {
        AssertName(directiveNode, typeNames.WebSocketDirective);
        AssertArguments(directiveNode, OptionalArgs, SubgraphArg, BaseAddressArg);

        string name = default!;
        string subgraph = default!;
        string baseAddress = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case ClientNameArg:
                    name = Expect<StringValueNode>(argument.Value).Value;
                    break;

                case SubgraphArg:
                    subgraph = Expect<StringValueNode>(argument.Value).Value;
                    break;

                case BaseAddressArg:
                    baseAddress = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        if (string.IsNullOrEmpty(name))
        {
            name = subgraph;
        }

        return new WebSocketClientConfiguration(name, subgraph, new Uri(baseAddress));

        static void OptionalArgs(HashSet<string> assert)
        {
            assert.Remove(ClientNameArg);
        }
    }

    private VariableDefinitionCollection ReadObjectVariableDefinitions(
        FusionTypeNames typeNames,
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        var definitions = new List<FieldVariableDefinition>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(typeNames.VariableDirective))
            {
                definitions.Add(ReadFieldVariableDefinition(typeNames, directiveNode));
            }
        }

        return new VariableDefinitionCollection(definitions);
    }

    private FieldVariableDefinitionCollection ReadFieldVariableDefinitions(
        FusionTypeNames typeNames,
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        var definitions = new List<IVariableDefinition>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(typeNames.VariableDirective))
            {
                if (directiveNode.Arguments.Any(t => t.Name.Value.EqualsOrdinal(ArgumentArg)))
                {
                    definitions.Add(ReadArgumentVariableDefinition(typeNames, directiveNode));
                }
                else
                {
                    definitions.Add(ReadFieldVariableDefinition(typeNames, directiveNode));
                }
            }
        }

        return new FieldVariableDefinitionCollection(definitions);
    }

    private ArgumentVariableDefinition ReadArgumentVariableDefinition(
        FusionTypeNames typeNames,
        DirectiveNode directiveNode)
    {
        AssertName(directiveNode, typeNames.VariableDirective);
        AssertArguments(directiveNode, NameArg, ArgumentArg, TypeArg, SubgraphArg);

        string name = default!;
        string argumentName = default!;
        ITypeNode type = default!;
        string schemaName = default!;

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

                case TypeArg:
                    type = ParseTypeReference(Expect<StringValueNode>(argument.Value).Value);
                    break;

                case SubgraphArg:
                    schemaName = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        return new ArgumentVariableDefinition(name, schemaName, type, argumentName);
    }

    private FieldVariableDefinition ReadFieldVariableDefinition(
        FusionTypeNames typeNames,
        DirectiveNode directiveNode)
    {
        AssertName(directiveNode, typeNames.VariableDirective);
        AssertArguments(directiveNode, NameArg, SelectArg, SubgraphArg);

        string name = default!;
        FieldNode select = default!;
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

                case SubgraphArg:
                    schemaName = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        return new FieldVariableDefinition(name, schemaName, select);
    }

    private ResolverDefinitionCollection ReadResolverDefinitions(
        FusionTypeNames typeNames,
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        List<ResolverDefinition>? definitions = null;

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(typeNames.ResolverDirective))
            {
                (definitions ??= new()).Add(ReadResolverDefinition(typeNames, directiveNode));
            }
        }

        return definitions is null
            ? ResolverDefinitionCollection.Empty
            : new ResolverDefinitionCollection(definitions);
    }

    private ResolverDefinition ReadResolverDefinition(
        FusionTypeNames typeNames,
        DirectiveNode directiveNode)
    {
        AssertName(directiveNode, typeNames.ResolverDirective);
        AssertArguments(directiveNode, OptionalArgs, SelectArg, SubgraphArg);

        SelectionSetNode select = default!;
        string subgraph = default!;
        var kind = ResolverKind.Query;
        Dictionary<string, ITypeNode>? arguments = null;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case SelectArg:
                    select = ParseSelectionSet(Expect<StringValueNode>(argument.Value).Value);
                    break;

                case SubgraphArg:
                    subgraph = Expect<StringValueNode>(argument.Value).Value;
                    break;

                case KindArg:
                    kind = Expect<StringValueNode>(argument.Value).Value switch
                    {
                        FusionEnumValueNames.Query => ResolverKind.Query,
                        FusionEnumValueNames.Batch => ResolverKind.Batch,
                        FusionEnumValueNames.BatchByKey => ResolverKind.BatchByKey,
                        FusionEnumValueNames.Subscription => ResolverKind.Subscription,
                        _ => throw new InvalidOperationException(
                            FusionResources
                                .FusionGraphConfigurationReader_ReadResolverDefinition_InvalidKindValue)
                    };
                    break;

                case ArgumentsArg:
                    arguments = ReadResolverArgumentDefinitions(argument.Value);
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

        return new ResolverDefinition(
            subgraph,
            kind,
            select,
            placeholder,
            _assert.Count == 0
                ? Array.Empty<string>()
                : _assert.ToArray(),
            arguments ?? _emptyArgumentDefs);

        static void OptionalArgs(HashSet<string> assert)
        {
            assert.Remove(KindArg);
            assert.Remove(ArgumentsArg);
        }
    }

    private Dictionary<string, ITypeNode>? ReadResolverArgumentDefinitions(
        IValueNode argumentDefinitions)
    {
        if (argumentDefinitions is NullValueNode)
        {
            return null;
        }

        var arguments = new Dictionary<string, ITypeNode>();

        foreach (var argumentDef in Expect<ListValueNode>(argumentDefinitions).Items)
        {
            var argumentDefNode = Expect<ObjectValueNode>(argumentDef);

            string argumentName = default!;
            ITypeNode argumentType = default!;

            foreach (var argumentDefArgument in argumentDefNode.Fields)
            {
                switch (argumentDefArgument.Name.Value)
                {
                    case NameArg:
                        argumentName = Expect<StringValueNode>(argumentDefArgument.Value).Value;
                        break;

                    case TypeArg:
                        argumentType = ParseTypeReference(
                            Expect<StringValueNode>(argumentDefArgument.Value).Value);
                        break;
                }
            }

            arguments.Add(argumentName, argumentType);
        }

        return arguments;
    }

    private MemberBindingCollection ReadMemberBindings(
        FusionTypeNames typeNames,
        IReadOnlyList<DirectiveNode> directiveNodes,
        NamedSyntaxNode annotatedMember)
    {
        List<MemberBinding>? definitions = null;

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(typeNames.SourceDirective))
            {
                var memberBinding = ReadMemberBinding(typeNames, directiveNode, annotatedMember);
                (definitions ??= new()).Add(memberBinding);
            }
        }

        return definitions is null
            ? MemberBindingCollection.Empty
            : new MemberBindingCollection(definitions);
    }

    private MemberBindingCollection ReadMemberBindings(
        FusionTypeNames typeNames,
        IReadOnlyList<DirectiveNode> directiveNodes,
        FieldDefinitionNode annotatedField,
        ResolverDefinitionCollection resolvers)
    {
        var definitions = new List<MemberBinding>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(typeNames.SourceDirective))
            {
                definitions.Add(ReadMemberBinding(typeNames, directiveNode, annotatedField));
            }
        }

        if (resolvers.Count > 0)
        {
            _assert.Clear();

            foreach (var binding in definitions)
            {
                _assert.Add(binding.SubgraphName);
            }

            foreach (var resolver in resolvers)
            {
                if (_assert.Add(resolver.SubgraphName))
                {
                    definitions.Add(
                        new MemberBinding(resolver.SubgraphName, annotatedField.Name.Value));
                }
            }
        }

        return new MemberBindingCollection(definitions);
    }

    private ObjectFieldFlags ReadFlags(
        FusionTypeNames typeNames,
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        var flags = ObjectFieldFlags.None;

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(typeNames.ReEncodeIdDirective))
            {
                flags |= ObjectFieldFlags.ReEncodeId;
                break;
            }
        }

        return flags;
    }

    private MemberBinding ReadMemberBinding(
        FusionTypeNames typeNames,
        DirectiveNode directiveNode,
        NamedSyntaxNode annotatedField)
    {
        AssertName(directiveNode, typeNames.SourceDirective);
        AssertArguments(directiveNode, SubgraphArg, NameArg);

        string? name = null;
        string subgraphName = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case NameArg:
                    name = Expect<StringValueNode>(argument.Value).Value;
                    break;

                case SubgraphArg:
                    subgraphName = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        _subgraphNames.Add(subgraphName);

        return new MemberBinding(subgraphName, name ?? annotatedField.Name.Value);
    }

    private static T Expect<T>(IValueNode valueNode) where T : IValueNode
    {
        if (valueNode is not T casted)
        {
            throw ServiceConfInvalidValue(typeof(T), valueNode);
        }

        return casted;
    }

    private static void AssertName(DirectiveNode directive, string expectedName)
    {
        if (!directive.Name.Value.EqualsOrdinal(expectedName))
        {
            throw ServiceConfInvalidDirectiveName(expectedName, directive.Name.Value);
        }
    }

    private void AssertArguments(DirectiveNode directive, params string[] expectedArguments)
        => AssertArguments(directive, null, expectedArguments);

    private void AssertArguments(
        DirectiveNode directive,
        Action<HashSet<string>>? beforeAssert,
        params string[] expectedArguments)
    {
        if (directive.Arguments.Count == 0)
        {
            throw ServiceConfNoDirectiveArgs(directive.Name.Value);
        }

        _assert.Clear();

        foreach (var argument in directive.Arguments)
        {
            _assert.Add(argument.Name.Value);
        }

        _assert.ExceptWith(expectedArguments);
        beforeAssert?.Invoke(_assert);

        if (_assert.Count > 0)
        {
            throw ServiceConfInvalidDirectiveArgs(
                directive.Name.Value,
                expectedArguments,
                _assert,
                directive.Location?.Line ?? -1);
        }
    }
}
