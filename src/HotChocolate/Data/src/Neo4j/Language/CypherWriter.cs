using System;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Data.Neo4j
{
    public class CypherWriter : IDisposable
    {
        private static ObjectPoolProvider _pool = new DefaultObjectPoolProvider();
        private readonly ObjectPool<StringBuilder> _stringBuilderPool = _pool.CreateStringBuilderPool();
        private readonly StringBuilder _writer;

        public CypherWriter()
        {
            _writer = _stringBuilderPool.Get();
        }

        /// <summary>
        /// Appends text.
        /// </summary>
        /// <param name="text">The text.</param>
        public void Append(string text)
        {
            _writer.Append(text);
        }

        public string Print()
        {
            return _writer.ToString();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _stringBuilderPool.Return(_writer);
        }
    }
}
