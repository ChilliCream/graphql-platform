using System.Collections.Generic;
using System.Globalization;
using StrawberryShake.VisualStudio.Language.Properties;

namespace StrawberryShake.VisualStudio.Language
{
    // Implements the parsing rules in the Type Definition section.
    public ref partial struct StringGraphQLClassifier
    {
        /// <summary>
        /// Parses a description.
        /// <see cref="StringValueNode" />:
        /// StringValue
        /// </summary>
        private void ParseDescription()
        {
            if (_isString[(int)_reader.Kind])
            {
                ParseStringLiteral(SyntaxClassificationKind.Comment);
            }
        }

        /// <summary>
        /// Parses a schema definition.
        /// <see cref="SchemaDefinitionNode" />:
        /// schema Directives[isConstant:true]? { OperationTypeDefinition+ }
        /// </summary>
        private void ParseSchemaDefinition()
        {
            classifications.AddClassification(
                SyntaxClassificationKind.SchemaKeyword,
                _reader.Token);
            MoveNext();

            ParseDirectives(true);

            if (_reader.Kind == TokenKind.LeftBrace)
            {
                classifications.AddClassification(
                    SyntaxClassificationKind.Brace,
                    _reader.Token);
                MoveNext();

                while (_reader.Kind != TokenKind.RightBrace)
                {
                    ParseOperationTypeDefinition();
                }

                ParseRightBrace();
            }
            else
            {
                classifications.AddClassification(
                    SyntaxClassificationKind.Error,
                    _reader.Token);
                MoveNext();
            }
        }

        /// <summary>
        /// Parses an operation type definition.
        /// <see cref="OperationTypeDefinitionNode" />:
        /// OperationType : NamedType
        /// </summary>
        private void ParseOperationTypeDefinition()
        {
            ParseOperationType();
            ParseColon();
            ParseNamedType();
        }

        /// <summary>
        /// Parses a scalar type definition.
        /// <see cref="ScalarTypeDefinitionNode" />:
        /// Description?
        /// scalar Name Directives[isConstant=true]?
        /// </summary>
        private void ParseScalarTypeDefinition()
        {
            classifications.AddClassification(
                SyntaxClassificationKind.ScalarKeyword,
                _reader.Token);
            MoveNext();

            ParseName(SyntaxClassificationKind.ScalarIdentifier);
            ParseDirectives(true);
        }

        /// <summary>
        /// Parses an object type definition.
        /// <see cref="ObjectTypeDefinitionNode" />:
        /// Description?
        /// type Name ImplementsInterfaces? Directives[isConstant=true]? FieldsDefinition?
        /// </summary>
        private void ParseObjectTypeDefinition()
        {
            classifications.AddClassification(
                SyntaxClassificationKind.TypeKeyword,
                _reader.Token);
            MoveNext();

            ParseName(SyntaxClassificationKind.TypeIdentifier);
            ParseImplementsInterfaces();
            ParseDirectives(true);
            ParseFieldsDefinition();
        }

        /// <summary>
        /// Parses implementing interfaces.
        /// <see cref="List{NamedTypeNode}" />:
        /// implements &amp;? NamedType
        /// </summary>
        private void ParseImplementsInterfaces()
        {
            if (SkipImplementsKeyword())
            {
                // skip optional leading ampersand.
                SkipAmpersand();

                do
                {
                    ParseNamedType();
                }
                while (SkipAmpersand());
            }
        }

        /// <summary>
        /// Parses field definitions of an interface type or object type
        /// <see cref="IReadOnlyList{FieldDefinitionNode}" />:
        /// { FieldDefinition+ }
        /// </summary>
        private void ParseFieldsDefinition()
        {
            if (_reader.Kind == TokenKind.LeftBrace)
            {
                classifications.AddClassification(
                    SyntaxClassificationKind.Brace,
                    _reader.Token);
                MoveNext();

                while (_reader.Kind != TokenKind.RightBrace)
                {
                    ParseFieldDefinition();
                }

                ParseRightBrace();
            }
        }




        /// <summary>
        /// Parses a interface type or object type field definition.
        /// <see cref="FieldDefinitionNode" />:
        /// Description?
        /// Name ArgumentsDefinition? : Type Directives[isConstant=true]?
        /// </summary>
        private void ParseFieldDefinition()
        {
            ParseDescription();
            ParseName(SyntaxClassificationKind.FieldIdentifier);
            ParseArgumentDefinitions();
            ParseColon();
            ParseTypeReference();
            ParseDirectives(true);
        }

        /// <summary>
        /// Parses field arguments.
        /// <see cref="List{InputValueDefinitionNode}" />:
        /// ( InputValueDefinition+ )
        /// </summary>
        private void ParseArgumentDefinitions()
        {
            if (_reader.Kind == TokenKind.LeftParenthesis)
            {
                classifications.AddClassification(
                    SyntaxClassificationKind.Parenthesis,
                    _reader.Token);
                MoveNext();

                while (_reader.Kind != TokenKind.RightParenthesis)
                {
                    ParseInputValueDefinition(
                        SyntaxClassificationKind.ArgumentIdentifier);
                }

                ParseRightParenthesis();
            }
        }

        /// <summary>
        /// Parses input value definitions.
        /// <see cref="InputValueDefinitionNode" />:
        /// Description? Name : Type DefaultValue? Directives[isConstant=true]?
        /// </summary>
        private void ParseInputValueDefinition(
            SyntaxClassificationKind classificationKind)
        {
            ParseDescription();
            ParseName(classificationKind);
            ParseColon();
            ParseTypeReference();

            if (SkipEqual())
            {
                ParseValueLiteral(true);
            }

            ParseDirectives(true);
        }

        /// <summary>
        /// Parses an interface type definition.
        /// <see cref="InterfaceTypeDefinition" />:
        /// Description? interface Name Directives[isConstant=true]?
        /// FieldsDefinition?
        /// </summary>
        private void ParseInterfaceTypeDefinition()
        {
            classifications.AddClassification(
                SyntaxClassificationKind.InterfaceKeyword,
                _reader.Token);
            MoveNext();

            ParseName(SyntaxClassificationKind.InterfaceIdentifier);
            ParseDirectives(true);
            ParseFieldsDefinition();
        }

        /// <summary>
        /// Parses an union type definition.
        /// <see cref="UnionTypeDefinitionNode" />:
        /// Description? union Name Directives[isConstant=true]?
        /// UnionMemberTypes?
        /// </summary>
        private void ParseUnionTypeDefinition()
        {
            classifications.AddClassification(
                SyntaxClassificationKind.UnionKeyword,
                _reader.Token);
            MoveNext();

            ParseName(SyntaxClassificationKind.UnionIdentifier);
            ParseDirectives(true);
            ParseUnionMemberTypes();
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
                ParseRightBrace();

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
                ParseRightBrace();

                return list;
            }

            return _emptyInputValues;
        }
    }
}
