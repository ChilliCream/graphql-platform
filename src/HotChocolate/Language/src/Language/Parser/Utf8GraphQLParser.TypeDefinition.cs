using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    // Implements the parsing rules in the Type Definition section.
    public ref partial struct Utf8GraphQLParser
    {
        private static readonly List<EnumValueDefinitionNode> emptyEnumValues =
            new List<EnumValueDefinitionNode>();
        private static readonly List<InputValueDefinitionNode> _emptyInputVals =
            new List<InputValueDefinitionNode>();
        private static List<FieldDefinitionNode> _emptyFieldDefinitions =
            new List<FieldDefinitionNode>();

        /// <summary>
        /// Parses a description.
        /// <see cref="StringValueNode" />:
        /// StringValue
        /// </summary>
        /// <param name="context">The parser context.</param>
        private StringValueNode? ParseDescription()
        {
            if (TokenHelper.IsDescription(in _reader))
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
        /// <param name="context">The parser context.</param>
        private SchemaDefinitionNode ParseSchemaDefinition()
        {
            TokenInfo start = Start();

            // skip schema keyword
            MoveNext();

            List<DirectiveNode> directives = ParseDirectives(true);

            if (_reader.Kind != TokenKind.LeftBrace)
            {
                throw new SyntaxException(_reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBrace,
                        TokenVisualizer.Visualize(in _reader)));
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

            Location? location = CreateLocation(in start);

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
        /// <param name="context">The parser context.</param>
        private OperationTypeDefinitionNode ParseOperationTypeDefinition()
        {
            TokenInfo start = Start();

            OperationType operation = ParseOperationType();
            ExpectColon();
            NamedTypeNode type = ParseNamedType();

            Location? location = CreateLocation(in start);

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
        /// <param name="context">The parser context.</param>
        private ScalarTypeDefinitionNode ParseScalarTypeDefinition()
        {
            TokenInfo start = Start();

            // skip scalar keyword
            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);

            Location? location = CreateLocation(in start);

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
        /// <param name="context">The parser context.</param>
        private ObjectTypeDefinitionNode ParseObjectTypeDefinition()
        {
            TokenInfo start = Start();

            // skip type keyword
            MoveNext();

            NameNode name = ParseName();
            List<NamedTypeNode> interfaces = ParseImplementsInterfaces();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<FieldDefinitionNode> fields = ParseFieldsDefinition();

            Location? location = CreateLocation(in start);

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
        /// <param name="context">The parser context.</param>
        private List<NamedTypeNode> ParseImplementsInterfaces()
        {
            var list = new List<NamedTypeNode>();

            if (SkipImplementsKeyword())
            {
                // skip optional leading amperdand.
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
        /// <param name="context">The parser context.</param>
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
        /// <param name="context">The parser context.</param>
        private FieldDefinitionNode ParseFieldDefinition()
        {
            TokenInfo start = Start();

            StringValueNode? description = ParseDescription();
            NameNode name = ParseName();
            List<InputValueDefinitionNode> arguments =
                ParseArgumentDefinitions();
            ExpectColon();
            ITypeNode type = ParseTypeReference();
            List<DirectiveNode> directives = ParseDirectives(true);

            Location? location = CreateLocation(in start);

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
        /// <param name="context">The parser context.</param>
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

            return _emptyInputVals;
        }

        /// <summary>
        /// Parses input value definitions.
        /// <see cref="InputValueDefinitionNode" />:
        /// Description? Name : Type DefaultValue? Directives[isConstant=true]?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private InputValueDefinitionNode ParseInputValueDefinition()
        {
            TokenInfo start = Start();

            StringValueNode? description = ParseDescription();
            NameNode name = ParseName();
            ExpectColon();
            ITypeNode type = ParseTypeReference();
            IValueNode? defaultValue = SkipEqual()
                ? ParseValueLiteral(true)
                : null;
            List<DirectiveNode> directives =
                ParseDirectives(true);

            Location? location = CreateLocation(in start);

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
        /// <see cref="InterfaceTypeDefinition" />:
        /// Description? interface Name Directives[isConstant=true]?
        /// FieldsDefinition?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private InterfaceTypeDefinitionNode ParseInterfaceTypeDefinition()
        {
            TokenInfo start = Start();

            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives =
                ParseDirectives(true);
            List<FieldDefinitionNode> fields =
                ParseFieldsDefinition();

            Location? location = CreateLocation(in start);

            return new InterfaceTypeDefinitionNode
            (
                location,
                name,
                TakeDescription(),
                directives,
                fields
            );
        }

        /// <summary>
        /// Parses an union type definition.
        /// <see cref="UnionTypeDefinitionNode" />:
        /// Description? union Name Directives[isConstant=true]?
        /// UnionMemberTypes?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private UnionTypeDefinitionNode ParseUnionTypeDefinition()
        {
            TokenInfo start = Start();

            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives =
                ParseDirectives(true);
            List<NamedTypeNode> types = ParseUnionMemberTypes();

            Location? location = CreateLocation(in start);

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
        /// <param name="context">The parser context.</param>
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
        /// <param name="context">The parser context.</param>
        private EnumTypeDefinitionNode ParseEnumTypeDefinition()
        {
            TokenInfo start = Start();

            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<EnumValueDefinitionNode> values = ParseEnumValuesDefinition();

            Location? location = CreateLocation(in start);

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
        /// <param name="context">The parser context.</param>
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

            return emptyEnumValues;
        }



        /// <summary>
        /// Parses an enum value definitions.
        /// <see cref="EnumValueDefinitionNode" />:
        /// Description? EnumValue Directives[isConstant=true]?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private EnumValueDefinitionNode ParseEnumValueDefinition()
        {
            TokenInfo start = Start();

            StringValueNode? description = ParseDescription();
            NameNode name = ParseName();
            List<DirectiveNode> directives =
                ParseDirectives(true);

            Location? location = CreateLocation(in start);

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
            TokenInfo start = Start();

            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives =
                ParseDirectives(true);
            List<InputValueDefinitionNode> fields =
                ParseInputFieldsDefinition();

            Location? location = CreateLocation(in start);

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

            return _emptyInputVals;
        }
    }
}
