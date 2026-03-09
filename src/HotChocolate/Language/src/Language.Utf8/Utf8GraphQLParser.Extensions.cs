namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLParser
{
    private static readonly List<OperationTypeDefinitionNode> s_emptyOpDefs = [];

    private ITypeSystemExtensionNode ParseTypeExtension()
    {
        var start = Start();

        // extensions do not have a description
        TakeDescription();

        MoveNext();

        if (_reader.Kind == TokenKind.Name && _reader.Value.Length > 0)
        {
            switch (_reader.Value[0])
            {
                case (byte)'s':
                    if (_reader.Value.SequenceEqual(GraphQLKeywords.Schema))
                    {
                        return ParseSchemaExtension(in start);
                    }
                    if (_reader.Value.SequenceEqual(GraphQLKeywords.Scalar))
                    {
                        return ParseScalarTypeExtension(in start);
                    }
                    break;

                case (byte)'t':
                    if (_reader.Value.SequenceEqual(GraphQLKeywords.Type))
                    {
                        return ParseObjectTypeExtension(in start);
                    }
                    break;

                case (byte)'i':
                    if (_reader.Value.SequenceEqual(GraphQLKeywords.Interface))
                    {
                        return ParseInterfaceTypeExtension(in start);
                    }
                    if (_reader.Value.SequenceEqual(GraphQLKeywords.Input))
                    {
                        return ParseInputObjectTypeExtension(in start);
                    }
                    break;

                case (byte)'u':
                    if (_reader.Value.SequenceEqual(GraphQLKeywords.Union))
                    {
                        return ParseUnionTypeExtension(in start);
                    }
                    break;

                case (byte)'e':
                    if (_reader.Value.SequenceEqual(GraphQLKeywords.Enum))
                    {
                        return ParseEnumTypeExtension(in start);
                    }
                    break;
            }
        }

        throw Unexpected(_reader.Kind);
    }

    /// <summary>
    /// Parse schema definition extension.
    /// <see cref="SchemaExtensionNode" />:
    /// * - extend schema Directives[Const]? { OperationTypeDefinition+ }
    /// * - extend schema Directives[Const]
    /// </summary>
    private SchemaExtensionNode ParseSchemaExtension(in TokenInfo start)
    {
        MoveNext();

        var directives = ParseDirectives(true);

        var operationTypeDefinitions =
            ParseOperationTypeDefs();

        if (directives.Count == 0 && operationTypeDefinitions.Count == 0)
        {
            throw Unexpected(_reader.Kind);
        }

        var location = CreateLocation(in start);

        return new SchemaExtensionNode
        (
            location,
            directives,
            operationTypeDefinitions
        );
    }

    private List<OperationTypeDefinitionNode> ParseOperationTypeDefs()
    {
        if (_reader.Kind == TokenKind.LeftBrace)
        {
            var list = new List<OperationTypeDefinitionNode>();

            // skip opening token
            MoveNext();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                list.Add(ParseOperationTypeDefinition());
            }

            // skip closing token
            ExpectRightBrace();

            return list;
        }

        return s_emptyOpDefs;
    }

    private ScalarTypeExtensionNode ParseScalarTypeExtension(
        in TokenInfo start)
    {
        MoveNext();

        var name = ParseName();
        var directives = ParseDirectives(true);
        if (directives.Count == 0)
        {
            throw Unexpected(_reader.Kind);
        }
        var location = CreateLocation(in start);

        return new ScalarTypeExtensionNode
        (
            location,
            name,
            directives
        );
    }

    private ObjectTypeExtensionNode ParseObjectTypeExtension(
        in TokenInfo start)
    {
        MoveNext();

        var name = ParseName();
        var interfaces = ParseImplementsInterfaces();
        var directives = ParseDirectives(true);
        var fields = ParseFieldsDefinition();
        var location = CreateLocation(in start);

        if (interfaces.Count == 0
            && directives.Count == 0
            && fields.Count == 0)
        {
            throw Unexpected(_reader.Kind);
        }

        return new ObjectTypeExtensionNode
        (
            location,
            name,
            directives,
            interfaces,
            fields
        );
    }

    private InterfaceTypeExtensionNode ParseInterfaceTypeExtension(
        in TokenInfo start)
    {
        MoveNext();

        var name = ParseName();
        var interfaces = ParseImplementsInterfaces();
        var directives = ParseDirectives(true);
        var fields = ParseFieldsDefinition();
        var location = CreateLocation(in start);

        if (directives.Count == 0
            && fields.Count == 0)
        {
            throw Unexpected(_reader.Kind);
        }

        return new InterfaceTypeExtensionNode
        (
            location,
            name,
            directives,
            interfaces,
            fields
        );
    }

    private UnionTypeExtensionNode ParseUnionTypeExtension(
        in TokenInfo start)
    {
        MoveNext();

        var name = ParseName();
        var directives = ParseDirectives(true);
        var types = ParseUnionMemberTypes();
        var location = CreateLocation(in start);

        if (directives.Count == 0 && types.Count == 0)
        {
            throw Unexpected(_reader.Kind);
        }

        return new UnionTypeExtensionNode
        (
            location,
            name,
            directives,
            types
        );
    }

    private EnumTypeExtensionNode ParseEnumTypeExtension(in TokenInfo start)
    {
        MoveNext();

        var name = ParseName();
        var directives = ParseDirectives(true);
        var values = ParseEnumValuesDefinition();
        var location = CreateLocation(in start);

        if (directives.Count == 0 && values.Count == 0)
        {
            throw Unexpected(_reader.Kind);
        }

        return new EnumTypeExtensionNode
        (
            location,
            name,
            directives,
            values
        );
    }

    private InputObjectTypeExtensionNode ParseInputObjectTypeExtension(
        in TokenInfo start)
    {
        MoveNext();

        var name = ParseName();
        var directives = ParseDirectives(true);
        var fields =
            ParseInputFieldsDefinition();
        var location = CreateLocation(in start);

        if (directives.Count == 0 && fields.Count == 0)
        {
            throw Unexpected(_reader.Kind);
        }

        return new InputObjectTypeExtensionNode
        (
            location,
            name,
            directives,
            fields
        );
    }
}
