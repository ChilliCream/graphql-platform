using System.Collections.Generic;
using System.Globalization;
using StrawberryShake.VisualStudio.Language.Properties;

namespace StrawberryShake.VisualStudio.Language
{
    // Implements the parsing rules in the Type Definition section.
    public ref partial struct StringGraphQLParser
    {
        private static readonly List<EnumValueDefinitionNode> _emptyEnumValues =
            new List<EnumValueDefinitionNode>();
        private static readonly List<InputValueDefinitionNode> _emptyInputValues =
            new List<InputValueDefinitionNode>();
        private static readonly List<FieldDefinitionNode> _emptyFieldDefinitions =
            new List<FieldDefinitionNode>();

        /// <summary>
        /// Parses a description.
        /// <see cref="StringValueNode" />:
        /// StringValue
        /// </summary>
        /// <param name="context">The parser context.</param>
        private StringValueNode? ParseDescription()
        {
            if (_isString[(int)_reader.Kind])
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
            ISyntaxToken start = _reader.Token;

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

            var location = new Location(start, _reader.Token);

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
            ISyntaxToken start = _reader.Token;

            OperationType operation = ParseOperationType();
            ExpectColon();
            NamedTypeNode type = ParseNamedType();

            var location = new Location(start, _reader.Token);

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
            ISyntaxToken start = _reader.Token;

            // skip scalar keyword
            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);

            var location = new Location(start, _reader.Token);

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
            ISyntaxToken start = _reader.Token;

            // skip type keyword
            MoveNext();

            NameNode name = ParseName();
            List<NamedTypeNode> interfaces = ParseImplementsInterfaces();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<FieldDefinitionNode> fields = ParseFieldsDefinition();

            var location = new Location(start, _reader.Token);

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
            ISyntaxToken start = _reader.Token;

            StringValueNode? description = ParseDescription();
            NameNode name = ParseName();
            List<InputValueDefinitionNode> arguments = ParseArgumentDefinitions();
            ExpectColon();
            ITypeNode type = ParseTypeReference();
            List<DirectiveNode> directives = ParseDirectives(true);

            var location = new Location(start, _reader.Token);

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

            return _emptyInputValues;
        }

        /// <summary>
        /// Parses input value definitions.
        /// <see cref="InputValueDefinitionNode" />:
        /// Description? Name : Type DefaultValue? Directives[isConstant=true]?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private InputValueDefinitionNode ParseInputValueDefinition()
        {
            ISyntaxToken start = _reader.Token;

            StringValueNode? description = ParseDescription();
            NameNode name = ParseName();
            ExpectColon();
            ITypeNode type = ParseTypeReference();
            IValueNode? defaultValue = SkipEqual()
                ? ParseValueLiteral(true)
                : null;
            List<DirectiveNode> directives = ParseDirectives(true);

            var location = new Location(start, _reader.Token);

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
            ISyntaxToken start = _reader.Token;

            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<FieldDefinitionNode> fields = ParseFieldsDefinition();

            var location = new Location(start, _reader.Token);

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
            ISyntaxToken start = _reader.Token;

            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<NamedTypeNode> types = ParseUnionMemberTypes();

            var location = new Location(start, _reader.Token);

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
            ISyntaxToken start = _reader.Token;

            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<EnumValueDefinitionNode> values = ParseEnumValuesDefinition();

            var location = new Location(start, _reader.Token);

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

            return _emptyEnumValues;
        }



        /// <summary>
        /// Parses an enum value definitions.
        /// <see cref="EnumValueDefinitionNode" />:
        /// Description? EnumValue Directives[isConstant=true]?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private EnumValueDefinitionNode ParseEnumValueDefinition()
        {
            ISyntaxToken start = _reader.Token;

            StringValueNode? description = ParseDescription();
            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);

            var location = new Location(start, _reader.Token);

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
            ISyntaxToken start = _reader.Token;

            MoveNext();

            NameNode name = ParseName();
            List<DirectiveNode> directives = ParseDirectives(true);
            List<InputValueDefinitionNode> fields = ParseInputFieldsDefinition();

            var location = new Location(start, _reader.Token);

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
}
