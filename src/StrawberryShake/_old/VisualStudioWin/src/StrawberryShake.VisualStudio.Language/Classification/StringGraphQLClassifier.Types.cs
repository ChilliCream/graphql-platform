namespace StrawberryShake.VisualStudio.Language
{
    // Implements the parsing rules in the Types section.
    public ref partial struct StringGraphQLClassifier
    {
        /// <summary>
        /// Parses a type reference.
        /// <see cref="ITypeNode" />:
        /// - NamedType
        /// - ListType
        /// - NonNullType
        /// </summary>
        private void ParseTypeReference()
        {
            if (_reader.Kind == TokenKind.LeftBracket)
            {
                _classifications.AddClassification(
                    SyntaxClassificationKind.Bracket,
                    _reader.Token);
                MoveNext();
                ParseTypeReference();
                ParseRightBracket();
            }
            else
            {
                ParseNamedType();
            }

            if (_reader.Kind == TokenKind.Bang)
            {
                _classifications.AddClassification(
                    SyntaxClassificationKind.Bang,
                    _reader.Token);
                MoveNext();
            }
        }

        /// <summary>
        /// Parses a named type.
        /// <see cref="NamedTypeNode" />:
        /// Name
        /// </summary>
        /// <param name="context">The parser context.</param>
        private void ParseNamedType() =>
            ParseName(SyntaxClassificationKind.TypeReference);
    }
}
