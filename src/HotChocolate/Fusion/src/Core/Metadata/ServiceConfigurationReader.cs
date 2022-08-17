using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Metadata.ConfigurationDirectiveNames;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Metadata;

internal sealed class ServiceConfigurationReader
{
    private readonly HashSet<string> _assert = new();

    public ServiceConfiguration Read(string sourceText)
        => ReadServiceDefinition(Utf8GraphQLParser.Parse(sourceText));

    public ServiceConfiguration Read(DocumentNode document)
        => ReadServiceDefinition(document);

    private ServiceConfiguration ReadServiceDefinition(DocumentNode documentNode)
    {
        var context = ConfigurationDirectiveNamesContext.From(documentNode);

        var types = new List<IType>();
        IReadOnlyList<HttpClientConfig>? httpClientConfigs = null;

        foreach (var definition in documentNode.Definitions)
        {
            switch (definition)
            {
                case ObjectTypeDefinitionNode node:
                    types.Add(ReadObjectType(context, node));
                    break;

                case SchemaDefinitionNode node:
                    httpClientConfigs = ReadHttpClientConfigs(context, node.Directives);
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

        return new ServiceConfiguration(types, httpClientConfigs);
    }

    private ObjectType ReadObjectType(
        ConfigurationDirectiveNamesContext context,
        ObjectTypeDefinitionNode typeDef)
    {
        var bindings = ReadMemberBindings(context, typeDef.Directives, typeDef);
        var variables = ReadFieldVariableDefinitions(context, typeDef.Directives);
        var resolvers = ReadFetchDefinitions(context, typeDef.Directives);
        var fields = ReadObjectFields(context, typeDef.Fields);
        return new ObjectType(typeDef.Name.Value, bindings, variables, resolvers, fields);
    }

    private ObjectFieldCollection ReadObjectFields(
        ConfigurationDirectiveNamesContext context,
        IReadOnlyList<FieldDefinitionNode> fieldDefinitionNodes)
    {
        var collection = new List<ObjectField>();

        foreach (var fieldDef in fieldDefinitionNodes)
        {
            var resolvers = ReadFetchDefinitions(context, fieldDef.Directives);
            var bindings = ReadMemberBindings(context, fieldDef.Directives, fieldDef, resolvers);
            var variables = ReadArgumentVariableDefinitions(context, fieldDef.Directives, fieldDef);
            var field = new ObjectField(fieldDef.Name.Value, bindings, variables, resolvers);
            collection.Add(field);
        }

        return new ObjectFieldCollection(collection);
    }

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
        AssertArguments(directiveNode, SchemaArg, BaseAddressArg);

        string name = default!;
        string baseAddress = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case SchemaArg:
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
        var definitions = new List<FieldVariableDefinition>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(context.VariableDirective))
            {
                definitions.Add(ReadFieldVariableDefinition(context, directiveNode));
            }
        }

        return new VariableDefinitionCollection(definitions);
    }

    private FieldVariableDefinition ReadFieldVariableDefinition(
        ConfigurationDirectiveNamesContext context,
        DirectiveNode directiveNode)
    {
        AssertName(directiveNode, context.VariableDirective);
        AssertArguments(directiveNode, NameArg, SelectArg, TypeArg, SchemaArg);

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

                case SchemaArg:
                    schemaName = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        return new FieldVariableDefinition(name, schemaName, type, select);
    }

    private FetchDefinitionCollection ReadFetchDefinitions(
        ConfigurationDirectiveNamesContext context,
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        var definitions = new List<FetchDefinition>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(context.FetchDirective))
            {
                definitions.Add(ReadFetchDefinition(context, directiveNode));
            }
        }

        return new FetchDefinitionCollection(definitions);
    }

    private FetchDefinition ReadFetchDefinition(
        ConfigurationDirectiveNamesContext context,
        DirectiveNode directiveNode)
    {
        AssertName(directiveNode, context.FetchDirective);
        AssertArguments(directiveNode, SelectArg, SchemaArg);

        ISelectionNode select = default!;
        string schemaName = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case SelectArg:
                    select = ParseField(Expect<StringValueNode>(argument.Value).Value);
                    break;

                case SchemaArg:
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
        ConfigurationDirectiveNamesContext context,
        IReadOnlyList<DirectiveNode> directiveNodes,
        NamedSyntaxNode annotatedMember)
    {
        var definitions = new List<MemberBinding>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(context.SourceDirective))
            {
                definitions.Add(ReadMemberBinding(context, directiveNode, annotatedMember));
            }
        }

        return new MemberBindingCollection(definitions);
    }

    private MemberBindingCollection ReadMemberBindings(
        ConfigurationDirectiveNamesContext context,
        IReadOnlyList<DirectiveNode> directiveNodes,
        FieldDefinitionNode annotatedField,
        FetchDefinitionCollection resolvers)
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
        ConfigurationDirectiveNamesContext context,
        DirectiveNode directiveNode,
        NamedSyntaxNode annotatedField)
    {
        AssertName(directiveNode, context.SourceDirective);
        AssertArguments(directiveNode, SchemaArg, NameArg);

        string? name = null;
        string schemaName = default!;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case NameArg:
                    name = Expect<StringValueNode>(argument.Value).Value;
                    break;

                case SchemaArg:
                    schemaName = Expect<StringValueNode>(argument.Value).Value;
                    break;
            }
        }

        return new MemberBinding(schemaName, name ?? annotatedField.Name.Value);
    }

    private ArgumentVariableDefinitionCollection ReadArgumentVariableDefinitions(
        ConfigurationDirectiveNamesContext context,
        IReadOnlyList<DirectiveNode> directiveNodes,
        FieldDefinitionNode annotatedField)
    {
        var definitions = new List<ArgumentVariableDefinition>();

        foreach (var directiveNode in directiveNodes)
        {
            if (directiveNode.Name.Value.EqualsOrdinal(context.VariableDirective))
            {
                definitions.Add(
                    ReadArgumentVariableDefinition(
                        context,
                        directiveNode,
                        annotatedField));
            }
        }

        return new ArgumentVariableDefinitionCollection(definitions);
    }

    private ArgumentVariableDefinition ReadArgumentVariableDefinition(
        ConfigurationDirectiveNamesContext context,
        DirectiveNode directiveNode,
        FieldDefinitionNode annotatedField)
    {
        AssertName(directiveNode, context.VariableDirective);
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
        if (directive.Arguments.Count == 0)
        {
            // TODO : EXCEPTION
            throw new InvalidOperationException(
                $"The directive `{directive.Name.Value}` has required arguments " +
                "but non were provided.");
        }

        _assert.Clear();

        foreach (var argument in directive.Arguments)
        {
            _assert.Add(argument.Name.Value);
        }

        _assert.ExceptWith(expectedArguments);

        if (_assert.Count > 0)
        {
            throw new InvalidOperationException(
                $"Expected arguments for the directive `{directive.Name.Value}` are " +
                $"`{string.Join(", ", expectedArguments)}`. " +
                "The service configuration reader found the following arguments " +
                $"`{string.Join(", ", _assert)}` on the directive in line number " +
                $"{directive.Location!.Line}.");
        }
    }
}
