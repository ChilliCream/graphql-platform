using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Metadata.ConfigurationDirectiveNames;
using static HotChocolate.Fusion.ThrowHelper;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Metadata;

internal sealed class FusionGraphConfigurationReader
{
    private readonly HashSet<string> _assert = new();

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
        var context = ConfigurationDirectiveNamesContext.From(document);
        var httpClientConfigs = ReadHttpClientConfigs(context, schemaDef.Directives);
        var typeNameField = CreateTypeNameField(httpClientConfigs.Select(t => t.SchemaName));

        foreach (var definition in document.Definitions)
        {
            switch (definition)
            {
                case ObjectTypeDefinitionNode node:
                    types.Add(ReadObjectType(context, node, typeNameField));
                    break;
            }
        }

        if (httpClientConfigs is not { Count: > 0 })
        {
            throw ServiceConfNoClientsSpecified();
        }

        if (types.Count == 0)
        {
            throw ServiceConfNoTypesSpecified();
        }

        return new FusionGraphConfiguration(types, httpClientConfigs);
    }

    private ObjectType ReadObjectType(
        ConfigurationDirectiveNamesContext context,
        ObjectTypeDefinitionNode typeDef,
        ObjectField typeNameField)
    {
        var bindings = ReadMemberBindings(context, typeDef.Directives, typeDef);
        var variables = ReadFieldVariableDefinitions(context, typeDef.Directives);
        var resolvers = ReadResolverDefinitions(context, typeDef.Directives);
        var fields = ReadObjectFields(context, typeDef.Fields, typeNameField);
        return new ObjectType(typeDef.Name.Value, bindings, variables, resolvers, fields);
    }

    private ObjectFieldCollection ReadObjectFields(
        ConfigurationDirectiveNamesContext context,
        IReadOnlyList<FieldDefinitionNode> fieldDefinitionNodes,
        ObjectField typeNameField)
    {
        var collection = new List<ObjectField>();

        foreach (var fieldDef in fieldDefinitionNodes)
        {
            var resolvers = ReadResolverDefinitions(context, fieldDef.Directives);
            var bindings = ReadMemberBindings(context, fieldDef.Directives, fieldDef, resolvers);
            var variables = ReadFieldVariableDefinitions(context, fieldDef.Directives);
            var field = new ObjectField(fieldDef.Name.Value, bindings, variables, resolvers);
            collection.Add(field);
        }

        collection.Add(typeNameField);

        return new ObjectFieldCollection(collection);
    }

    private static ObjectField CreateTypeNameField(IEnumerable<string> schemaNames)
        => new ObjectField(
            IntrospectionFields.TypeName,
            new MemberBindingCollection(
                schemaNames.Select(t => new MemberBinding(t, IntrospectionFields.TypeName))),
            VariableDefinitionCollection.Empty,
            ResolverDefinitionCollection.Empty);

    private IReadOnlyList<HttpClientConfig> ReadHttpClientConfigs(
        ConfigurationDirectiveNamesContext context,
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        var configs = new List<HttpClientConfig>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(context.HttpDirective))
            {
                configs.Add(ReadHttpClientConfig(context, directiveNode));
            }
        }

        return configs;
    }

    private HttpClientConfig ReadHttpClientConfig(
        ConfigurationDirectiveNamesContext context,
        DirectiveNode directiveNode)
    {
        AssertName(directiveNode, context.HttpDirective);
        AssertArguments(directiveNode, SubGraphArg, BaseAddressArg);

        string name = default!;
        string baseAddress = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case SubGraphArg:
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
        ConfigurationDirectiveNamesContext context,
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        var definitions = new List<VariableDefinition>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(context.VariableDirective))
            {
                definitions.Add(ReadFieldVariableDefinition(context, directiveNode));
            }
        }

        return new VariableDefinitionCollection(definitions);
    }

    private VariableDefinition ReadFieldVariableDefinition(
        ConfigurationDirectiveNamesContext context,
        DirectiveNode directiveNode)
    {
        AssertName(directiveNode, context.VariableDirective);
        AssertArguments(directiveNode, NameArg, SelectArg, TypeArg, SubGraphArg);

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

                case SubGraphArg:
                    schemaName = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        return new VariableDefinition(name, schemaName, type, select);
    }

    private ResolverDefinitionCollection ReadResolverDefinitions(
        ConfigurationDirectiveNamesContext context,
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        List<ResolverDefinition>? definitions = null;

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(context.FetchDirective))
            {
                (definitions ??= new()).Add(ReadResolverDefinition(context, directiveNode));
            }
        }

        return definitions is null
            ? ResolverDefinitionCollection.Empty
            : new ResolverDefinitionCollection(definitions);
    }

    private ResolverDefinition ReadResolverDefinition(
        ConfigurationDirectiveNamesContext context,
        DirectiveNode directiveNode)
    {
        AssertName(directiveNode, context.FetchDirective);
        AssertArguments(directiveNode, SelectArg, SubGraphArg);

        SelectionSetNode select = default!;
        string subGraph = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case SelectArg:
                    select = ParseSelectionSet(Expect<StringValueNode>(argument.Value).Value);
                    break;

                case SubGraphArg:
                    subGraph = Expect<StringValueNode>(argument.Value).Value;
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
            subGraph,
            select,
            placeholder,
            _assert.Count == 0
                ? Array.Empty<string>()
                : _assert.ToArray());
    }

    private MemberBindingCollection ReadMemberBindings(
        ConfigurationDirectiveNamesContext context,
        IReadOnlyList<DirectiveNode> directiveNodes,
        NamedSyntaxNode annotatedMember)
    {
        List<MemberBinding>? definitions = null;

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(context.SourceDirective))
            {
                var memberBinding = ReadMemberBinding(context, directiveNode, annotatedMember);
                (definitions ??= new()).Add(memberBinding);
            }
        }

        return definitions is null
            ? MemberBindingCollection.Empty
            : new MemberBindingCollection(definitions);
    }

    private MemberBindingCollection ReadMemberBindings(
        ConfigurationDirectiveNamesContext context,
        IReadOnlyList<DirectiveNode> directiveNodes,
        FieldDefinitionNode annotatedField,
        ResolverDefinitionCollection resolvers)
    {
        var definitions = new List<MemberBinding>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(context.SourceDirective))
            {
                definitions.Add(ReadMemberBinding(context, directiveNode, annotatedField));
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
                if (_assert.Add(resolver.SubGraphName))
                {
                    definitions.Add(
                        new MemberBinding(resolver.SubGraphName, annotatedField.Name.Value));
                }
            }
        }

        return new MemberBindingCollection(definitions);
    }

    private MemberBinding ReadMemberBinding(
        ConfigurationDirectiveNamesContext context,
        DirectiveNode directiveNode,
        NamedSyntaxNode annotatedField)
    {
        AssertName(directiveNode, context.SourceDirective);
        AssertArguments(directiveNode, SubGraphArg, NameArg);

        string? name = null;
        string schemaName = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case NameArg:
                    name = Expect<StringValueNode>(argument.Value).Value;
                    break;

                case SubGraphArg:
                    schemaName = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        return new MemberBinding(schemaName, name ?? annotatedField.Name.Value);
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
