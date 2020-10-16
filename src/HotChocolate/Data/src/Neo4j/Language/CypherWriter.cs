using System;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Data.Neo4j
{
    public class CypherWriter : IDisposable
    {
        private bool isDisposed;
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
        public void Write(string text)
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
            Dispose(true);
            GC.SuppressFinalize(this);
            
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            if (disposing)
            {
                // free managed resources
                _stringBuilderPool.Return(_writer);
            }

            isDisposed = true;
        }
    }
}
