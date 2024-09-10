using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language;

// Implements the parsing rules in the Type Definition section.
public ref partial struct Utf8GraphQLParser
{
    private static readonly List<EnumValueDefinitionNode> _emptyEnumValues = [];
    private static readonly List<InputValueDefinitionNode> _emptyInputValues = [];
    private static readonly List<FieldDefinitionNode> _emptyFieldDefinitions = [];

    /// <summary>
    /// Parses a description.
    /// <see cref="StringValueNode" />:
    /// StringValue
    /// </summary>
    private StringValueNode? ParseDescription()
    {
        if (TokenHelper.IsDescription(ref _reader))
        {
            return ParseStringLiteral();
        }
        return null;
    }

    /// <summary>
    /// Parses a schema definition.
    /// <see cref="SchemaDefinitionNode" />:
    /// schema Directives[isConstant:true]? { OperationTypeDefinition+ }
    /// </summary>
    private SchemaDefinitionNode ParseSchemaDefinition()
    {
        var start = Start();

        // skip schema keyword
        MoveNext();

        var directives = ParseDirectives(true);

        if (_reader.Kind != TokenKind.LeftBrace)
        {
            throw new SyntaxException(_reader,
                ParseMany_InvalidOpenToken,
                TokenKind.LeftBrace,
                TokenPrinter.Print(ref _reader));
        }

        var operationTypeDefinitions =
            new List<OperationTypeDefinitionNode>();

        // skip opening token
        MoveNext();

        while (_reader.Kind != TokenKind.RightBrace)
        {
            operationTypeDefinitions.Add(ParseOperationTypeDefinition());
        }

        // skip closing token
        ExpectRightBrace();

        var location = CreateLocation(in start);

        return new SchemaDefinitionNode
        (
            location,
            TakeDescription(),
            directives,
            operationTypeDefinitions
        );
    }

    /// <summary>
    /// Parses an operation type definition.
    /// <see cref="OperationTypeDefinitionNode" />:
    /// OperationType : NamedType
    /// </summary>
    private OperationTypeDefinitionNode ParseOperationTypeDefinition()
    {
        var start = Start();

        var operation = ParseOperationType();
        ExpectColon();
        var type = ParseNamedType();

        var location = CreateLocation(in start);

        return new OperationTypeDefinitionNode
        (
            location,
            operation,
            type
        );
    }

    /// <summary>
    /// Parses a scalar type definition.
    /// <see cref="ScalarTypeDefinitionNode" />:
    /// Description?
    /// scalar Name Directives[isConstant=true]?
    /// </summary>
    private ScalarTypeDefinitionNode ParseScalarTypeDefinition()
    {
        var start = Start();

        // skip scalar keyword
        MoveNext();

        var name = ParseName();
        var directives = ParseDirectives(true);

        var location = CreateLocation(in start);

        return new ScalarTypeDefinitionNode
        (
            location,
            name,
            TakeDescription(),
            directives
        );
    }

    /// <summary>
    /// Parses an object type definition.
    /// <see cref="ObjectTypeDefinitionNode" />:
    /// Description?
    /// type Name ImplementsInterfaces? Directives[isConstant=true]? FieldsDefinition?
    /// </summary>
    private ObjectTypeDefinitionNode ParseObjectTypeDefinition()
    {
        var start = Start();

        // skip type keyword
        MoveNext();

        var name = ParseName();
        var interfaces = ParseImplementsInterfaces();
        var directives = ParseDirectives(true);
        var fields = ParseFieldsDefinition();

        var location = CreateLocation(in start);

        return new ObjectTypeDefinitionNode
        (
            location,
            name,
            TakeDescription(),
            directives,
            interfaces,
            fields
        );
    }

    /// <summary>
    /// Parses implementing interfaces.
    /// <see cref="List{NamedTypeNode}" />:
    /// implements &amp;? NamedType
    /// </summary>
    private List<NamedTypeNode> ParseImplementsInterfaces()
    {
        var list = new List<NamedTypeNode>();

        if (SkipImplementsKeyword())
        {
            // skip optional leading ampersand.
            SkipAmpersand();

            do
            {
                list.Add(ParseNamedType());
            }
            while (SkipAmpersand());
        }

        return list;
    }

    /// <summary>
    /// Parses field definitions of an interface type or object type
    /// <see cref="IReadOnlyList{FieldDefinitionNode}" />:
    /// { FieldDefinition+ }
    /// </summary>
    private List<FieldDefinitionNode> ParseFieldsDefinition()
    {
        if (_reader.Kind == TokenKind.LeftBrace)
        {
            var list = new List<FieldDefinitionNode>();

            // skip opening token
            MoveNext();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                list.Add(ParseFieldDefinition());
            }

            // skip closing token
            ExpectRightBrace();

            return list;
        }
        return _emptyFieldDefinitions;
    }

    /// <summary>
    /// Parses a interface type or object type field definition.
    /// <see cref="FieldDefinitionNode" />:
    /// Description?
    /// Name ArgumentsDefinition? : Type Directives[isConstant=true]?
    /// </summary>
    private FieldDefinitionNode ParseFieldDefinition()
    {
        var start = Start();

        var description = ParseDescription();
        var name = ParseName();
        var arguments = ParseArgumentDefinitions();
        ExpectColon();
        var type = ParseTypeReference();
        var directives = ParseDirectives(true);

        var location = CreateLocation(in start);

        return new FieldDefinitionNode
        (
            location,
            name,
            description,
            arguments,
            type,
            directives
        );
    }

    /// <summary>
    /// Parses field arguments.
    /// <see cref="List{InputValueDefinitionNode}" />:
    /// ( InputValueDefinition+ )
    /// </summary>
    private List<InputValueDefinitionNode> ParseArgumentDefinitions()
    {
        if (_reader.Kind == TokenKind.LeftParenthesis)
        {
            var list = new List<InputValueDefinitionNode>();

            // skip opening token
            MoveNext();

            while (_reader.Kind != TokenKind.RightParenthesis)
            {
                list.Add(ParseInputValueDefinition());
            }

            // skip closing token
            ExpectRightParenthesis();

            return list;
        }

        return _emptyInputValues;
    }

    /// <summary>
    /// Parses input value definitions.
    /// <see cref="InputValueDefinitionNode" />:
    /// Description? Name : Type DefaultValue? Directives[isConstant=true]?
    /// </summary>
    private InputValueDefinitionNode ParseInputValueDefinition()
    {
        var start = Start();

        var description = ParseDescription();
        var name = ParseName();
        ExpectColon();
        var type = ParseTypeReference();
        var defaultValue = SkipEqual()
            ? ParseValueLiteral(true)
            : null;
        var directives =
            ParseDirectives(true);

        var location = CreateLocation(in start);

        return new InputValueDefinitionNode
        (
            location,
            name,
            description,
            type,
            defaultValue,
            directives
        );
    }

    /// <summary>
    /// Parses an interface type definition.
    /// <see cref="InterfaceTypeDefinitionNode" />:
    /// Description? interface Name Directives[isConstant=true]?
    /// FieldsDefinition?
    /// </summary>
    private InterfaceTypeDefinitionNode ParseInterfaceTypeDefinition()
    {
        var start = Start();

        MoveNext();

        var name = ParseName();
        var interfaces = ParseImplementsInterfaces();
        var directives = ParseDirectives(true);
        var fields = ParseFieldsDefinition();

        var location = CreateLocation(in start);

        return new InterfaceTypeDefinitionNode
        (
            location,
            name,
            TakeDescription(),
            directives,
            interfaces,
            fields
        );
    }

    /// <summary>
    /// Parses an union type definition.
    /// <see cref="UnionTypeDefinitionNode" />:
    /// Description? union Name Directives[isConstant=true]?
    /// UnionMemberTypes?
    /// </summary>
    private UnionTypeDefinitionNode ParseUnionTypeDefinition()
    {
        var start = Start();

        MoveNext();

        var name = ParseName();
        var directives = ParseDirectives(true);
        var types = ParseUnionMemberTypes();

        var location = CreateLocation(in start);

        return new UnionTypeDefinitionNode
        (
            location,
            name,
            TakeDescription(),
            directives,
            types
        );
    }

    /// <summary>
    /// Parses the union member types.
    /// <see cref="List{NamedTypeNode}" />:
    /// = `|`? NamedType
    /// </summary>
    private List<NamedTypeNode> ParseUnionMemberTypes()
    {
        var list = new List<NamedTypeNode>();

        if (SkipEqual())
        {
            // skip optional leading pipe (might not exist!)
            SkipPipe();

            do
            {
                list.Add(ParseNamedType());
            }
            while (SkipPipe());
        }

        return list;
    }

    /// <summary>
    /// Parses an enum type definition.
    /// <see cref="EnumTypeDefinitionNode" />:
    /// Description? enum Name Directives[Const]? EnumValuesDefinition?
    /// </summary>
    private EnumTypeDefinitionNode ParseEnumTypeDefinition()
    {
        var start = Start();

        MoveNext();

        var name = ParseName();
        var directives = ParseDirectives(true);
        var values = ParseEnumValuesDefinition();

        var location = CreateLocation(in start);

        return new EnumTypeDefinitionNode
        (
            location,
            name,
            TakeDescription(),
            directives,
            values
        );
    }

    /// <summary>
    /// Parses the value definitions of an enum type definition.
    /// <see cref="List{EnumValueDefinitionNode}" />:
    /// { EnumValueDefinition+ }
    /// </summary>
    private List<EnumValueDefinitionNode> ParseEnumValuesDefinition()
    {
        if (_reader.Kind == TokenKind.LeftBrace)
        {
            var list = new List<EnumValueDefinitionNode>();

            // skip opening token
            MoveNext();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                list.Add(ParseEnumValueDefinition());
            }

            // skip closing token
            ExpectRightBrace();

            return list;
        }

        return _emptyEnumValues;
    }

    /// <summary>
    /// Parses an enum value definitions.
    /// <see cref="EnumValueDefinitionNode" />:
    /// Description? EnumValue Directives[isConstant=true]?
    /// </summary>
    private EnumValueDefinitionNode ParseEnumValueDefinition()
    {
        var start = Start();

        var description = ParseDescription();
        var name = ParseName();
        var directives = ParseDirectives(true);

        var location = CreateLocation(in start);

        return new EnumValueDefinitionNode
        (
            location,
            name,
            description,
            directives
        );
    }

    private InputObjectTypeDefinitionNode ParseInputObjectTypeDefinition()
    {
        var start = Start();

        MoveNext();

        var name = ParseName();
        var directives = ParseDirectives(true);
        var fields = ParseInputFieldsDefinition();

        var location = CreateLocation(in start);

        return new InputObjectTypeDefinitionNode
        (
            location,
            name,
            TakeDescription(),
            directives,
            fields
        );
    }

    private List<InputValueDefinitionNode> ParseInputFieldsDefinition()
    {
        if (_reader.Kind == TokenKind.LeftBrace)
        {
            var list = new List<InputValueDefinitionNode>();

            // skip opening token
            MoveNext();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                list.Add(ParseInputValueDefinition());
            }

            // skip closing token
            ExpectRightBrace();

            return list;
        }

        return _emptyInputValues;
    }
}
