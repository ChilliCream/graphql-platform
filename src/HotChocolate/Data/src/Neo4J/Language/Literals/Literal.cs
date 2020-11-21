namespace HotChocolate.Data.Neo4J.Language
{
    public interface ILiteral<out T> : IExpression { }
    /// <summary>
    /// Represents a literal with an optional content.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Literal<T> : Expression, ILiteral<T>
    {
        /// <summary>
        /// The content of this literal.
        /// </summary>
        private readonly T _content;

        /// <summary>
        /// @return the content of this literal, result maybe null.
        /// </summary>
        /// <param name="content"></param>
        protected Literal(T content)
        {
            _content = content;
        }

        /// <returns>The content of this literal, may be null</returns>
        public T GetContent() => _content;

        /// <summary>
        /// The string representation should be designed in such a way the a renderer can use it correctly in
        /// the given context of the literal, i.e. a literal containing a string should quote that string
        /// and escape all reserved characters.
        /// </summary>
        /// <returns>A string representation to be used literally in a cypher statement.</returns>
        public abstract string AsString();
    }

    public abstract class Literal { }
}