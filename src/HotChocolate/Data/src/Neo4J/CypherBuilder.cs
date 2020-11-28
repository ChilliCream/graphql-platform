using System;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Data.Neo4J
{
    /// <summary>
    /// Responsible for abstracting the logic of building cypher query text.
    /// </summary>
    internal sealed class CypherBuilder : IDisposable
    {
        private bool _isDisposed;
        private static readonly ObjectPoolProvider _objectPoolProvider =
            new DefaultObjectPoolProvider();
        private static readonly ObjectPool<StringBuilder> _stringBuilderPool =
            _objectPoolProvider.CreateStringBuilderPool();
        private readonly StringBuilder _builder;

        public CypherBuilder()
        {
            _builder = _stringBuilderPool.Get();
        }

        /// <summary>
        /// Appends text.
        /// /// </summary>
        /// <param name="text">The text.</param>
        public void Write(string text)
        {
            _builder.Append(text);
        }

        public string Print()
        {
            return _builder.ToString();
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
        private void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                // free managed resources
                _stringBuilderPool.Return(_builder);
            }

            _isDisposed = true;
        }
    }
}
