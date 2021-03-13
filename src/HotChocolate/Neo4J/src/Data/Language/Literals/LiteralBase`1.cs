namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class LiteralBase<T> : Literal<T>
    {
        /// <summary>
        /// The content of this literal.
        /// </summary>
        private T _content;

        public LiteralBase(T content) : base(content)
        {
            _content = content;
        }

        public override string AsString()
        {
            throw new System.NotImplementedException();
        }
    }
}
