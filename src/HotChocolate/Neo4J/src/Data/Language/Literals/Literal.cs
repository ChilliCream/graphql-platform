namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class Literal<T> : Expression, ILiteral
    {
        /// <summary>
        /// @return the content of this literal, result maybe null.
        /// </summary>
        /// <param name="content"></param>
        protected Literal(T content)
        {
            Content = content;
        }

        /// <summary>
        /// The content of this literal.
        /// </summary>
        public T Content { get; }

        /// <summary>
        /// Represents a literal with an optional content.
        /// </summary>
        public override ClauseKind Kind => ClauseKind.Literal;

        /// <summary>
        /// The string representation should be designed in such a way the a renderer can use it
        /// correctly in the given context of the literal, i.e. a literal containing a string should
        /// quote that string and escape all reserved characters.
        /// </summary>
        /// <returns>
        /// A string representation to be used literally in a cypher statement.
        /// </returns>
        public abstract string Print();
    }
}
