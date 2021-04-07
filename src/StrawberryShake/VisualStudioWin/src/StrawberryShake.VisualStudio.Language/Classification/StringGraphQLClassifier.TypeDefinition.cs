using System.Collections.Generic;

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
                ParseStringLiteral(SyntaxClassificationKind.Description);
            }
        }

        /// <summary>
        /// Parses a schema definition.
        /// <see cref="SchemaDefinitionNode" />:
        /// schema Directives[isConstant:true]? { OperationTypeDefinition+ }
        /// </summary>
        private void ParseSchemaDefinition()
        {
            _classifications.AddClassification(
                SyntaxClassificationKind.SchemaKeyword,
                _reader.Token);
            MoveNext();

            ParseDirectives(true);

            if (_reader.Kind == TokenKind.LeftBrace)
            {
                _classifications.AddClassification(
                    SyntaxClassificationKind.Brace,
                    _reader.Token);
                MoveNext();

                while (_reader.Kind != TokenKind.RightBrace
                    && _reader.Kind != TokenKind.EndOfFile)
                {
                    ParseOperationTypeDefinition();
                }

                ParseRightBrace();
            }
            else
            {
                _classifications.AddClassification(
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
            _classifications.AddClassification(
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
            _classifications.AddClassification(
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
                _classifications.AddClassification(
                    SyntaxClassificationKind.Brace,
                    _reader.Token);
                MoveNext();

                while (_reader.Kind != TokenKind.RightBrace
                    && _reader.Kind != TokenKind.EndOfFile)
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
                _classifications.AddClassification(
                    SyntaxClassificationKind.Parenthesis,
                    _reader.Token);
                MoveNext();

                while (_reader.Kind != TokenKind.RightParenthesis
                    && _reader.Kind != TokenKind.EndOfFile)
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
            _classifications.AddClassification(
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
            _classifications.AddClassification(
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
        private void ParseUnionMemberTypes()
        {
            if (SkipEqual())
            {
                SkipPipe();

                do
                {
                    ParseNamedType();
                }
                while (SkipPipe());
            }
        }

        /// <summary>
        /// Parses an enum type definition.
        /// <see cref="EnumTypeDefinitionNode" />:
        /// Description? enum Name Directives[Const]? EnumValuesDefinition?
        /// </summary>
        private void ParseEnumTypeDefinition()
        {
            _classifications.AddClassification(
                SyntaxClassificationKind.EnumKeyword,
                _reader.Token);
            MoveNext();

            ParseName(SyntaxClassificationKind.EnumIdentifier);
            ParseDirectives(true);
            ParseEnumValuesDefinition();
        }

        /// <summary>
        /// Parses the value definitions of an enum type definition.
        /// <see cref="List{EnumValueDefinitionNode}" />:
        /// { EnumValueDefinition+ }
        /// </summary>
        private void ParseEnumValuesDefinition()
        {
            if (_reader.Kind == TokenKind.LeftBrace)
            {
                _classifications.AddClassification(
                    SyntaxClassificationKind.Brace,
                    _reader.Token);
                MoveNext();

                while (_reader.Kind != TokenKind.RightBrace
                    && _reader.Kind != TokenKind.EndOfFile)
                {
                    ParseEnumValueDefinition();
                }

                ParseRightBrace();
            }
        }

        /// <summary>
        /// Parses an enum value definitions.
        /// <see cref="EnumValueDefinitionNode" />:
        /// Description? EnumValue Directives[isConstant=true]?
        /// </summary>
        private void ParseEnumValueDefinition()
        {
            ParseDescription();
            ParseName(SyntaxClassificationKind.EnumLiteral);
            ParseDirectives(true);
        }

        private void ParseInputObjectTypeDefinition()
        {
            _classifications.AddClassification(
                SyntaxClassificationKind.InputKeyword,
                _reader.Token);
            MoveNext();

            ParseName(SyntaxClassificationKind.InputIdentifier);
            ParseDirectives(true);
            ParseInputFieldsDefinition();
        }

        private void ParseInputFieldsDefinition()
        {
            if (_reader.Kind == TokenKind.LeftBrace)
            {
                _classifications.AddClassification(
                    SyntaxClassificationKind.Brace,
                    _reader.Token);
                MoveNext();

                while (_reader.Kind != TokenKind.RightBrace
                    && _reader.Kind != TokenKind.EndOfFile)
                {
                    ParseInputValueDefinition(
                        SyntaxClassificationKind.InputFieldIdentifier);
                }

                ParseRightBrace();
            }
        }
    }
}
