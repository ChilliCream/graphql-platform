namespace HotChocolate.Language
{
    /// <summary>
    /// Represents a GraphQL syntax token.
    /// </summary>
    public sealed class SyntaxToken
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:HotChocolate.Language.Token"/> class.
        /// </summary>
        /// <param name="kind">
        /// The token kind.
        /// </param>
        /// <param name="start">
        /// The start index of this token.
        /// </param>
        /// <param name="end">
        /// The end index of this token.
        /// </param>
        /// <param name="line">
        /// The 1-base line index in which this token is located.
        /// </param>
        /// <param name="column">
        /// The 1-base column index in which this token is located.
        /// </param>
        /// <param name="previous">
        /// The token that came before this token.
        /// </param>
        public SyntaxToken(
            TokenKind kind,
            int start, int end,
            int line, int column,
            SyntaxToken previous)
        {
            Kind = kind;
            Start = start;
            End = end;
            Line = line;
            Column = column;
            Previous = previous;
        }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:HotChocolate.Language.Token"/> class.
        /// </summary>
        /// <param name="kind">
        /// The token kind.
        /// </param>
        /// <param name="start">
        /// The start index of this token.
        /// </param>
        /// <param name="end">
        /// The end index of this token.
        /// </param>
        /// <param name="line">
        /// The 1-base line index in which this token is located.
        /// </param>
        /// <param name="column">
        /// The 1-base column index in which this token is located.
        /// </param>
        /// <param name="value">
        /// The token value.
        /// </param>
        /// <param name="previous">
        /// The token that came before this token.
        /// </param>
        public SyntaxToken(
           TokenKind kind,
           int start, int end,
           int line, int column,
           string value,
           SyntaxToken previous)
        {
            Kind = kind;
            Start = start;
            End = end;
            Line = line;
            Column = column;
            Value = value;
            Previous = previous;
        }

        /// <summary>
        /// Gets the kind of <see cref="SyntaxToken" />.
        /// </summary>
        public TokenKind Kind { get; }

        /// <summary>
        /// Gets the character offset at which this node begins.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the character offset at which this node ends.
        /// </summary>
        public int End { get; }

        /// <summary>
        /// Gets the 1-indexed line number on which this 
        /// <see cref="SyntaxToken" /> appears.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Gets the 1-indexed column number at which this 
        /// <see cref="SyntaxToken" /> begins.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// For non-punctuation tokens, represents the interpreted 
        /// value of the token.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the token that came before this token.
        /// If this token is a
        /// <see cref="TokenKind.StartOfFile"/>-token than
        /// this property will return <c>null</c>.
        /// </summary>
        public SyntaxToken Previous { get; }

        /// <summary>
        /// Gets the token that comes after this token.
        /// If this token is a
        /// <see cref="TokenKind.EndOfFile"/>-token than
        /// this property will return <c>null</c>.
        /// </summary>
        public SyntaxToken Next { get; internal set; }

        /// <summary>
        /// Returns a <see cref="T:System.String"/>
        /// that represents the current
        /// <see cref="T:HotChocolate.Language.Token"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/>
        /// that represents the current
        /// <see cref="T:HotChocolate.Language.Token"/>.
        /// </returns>
        public override string ToString()
        {
            return TokenVisualizer.Visualize(this);
        }
    }
}
