using System;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4j
{
    public class CypherQuery
    {
        /// <summary>
        /// Is the query of type write or read.
        /// </summary>
        public bool IsWrite { get; private set; }

        /// <summary>
        /// The query text.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// The query parameters.
        /// </summary>
        public CypherParameters? Parameters { get; private set; }

        /// <summary>
        /// Query constructor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="isWrite">Default: false</param>
        public CypherQuery(string text, bool isWrite = false) : this(text, null, isWrite) { }

        /// <summary>
        /// Query constructor.
        /// </summary>
        /// <param name="text">The query text</param>
        /// <param name="parameters">the query parameters</param>
        /// <param name="isWrite">Weather the query is write or read</param>
        public CypherQuery(
            string text,
            CypherParameters? parameters,
            bool isWrite = false)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Parameters = parameters ?? new CypherParameters();
            IsWrite = isWrite;
        }

        public override string ToString() => Text;

        //public async Task<IResultCursor> ExecuteAsync()
        //{
        //    if (client.Connection == default)
        //    {
        //        throw new InvalidOperationException();
        //    }

        //    IAsyncSession session = client.Connection.AsyncSession();
        //    IResultCursor cursor;

        //    if (IsWrite)
        //    {
        //        cursor = Parameters != default ?
        //            await session.WriteTransactionAsync(async tx => await tx.RunAsync(Text, Parameters)
        //                .ConfigureAwait(false)).ConfigureAwait(false) :
        //                    await session.WriteTransactionAsync(async tx => await tx.RunAsync(Text)
        //                    .ConfigureAwait(false)).ConfigureAwait(false);

        //    }
        //    else
        //    {
        //        cursor = Parameters != default ?
        //            await session.ReadTransactionAsync(async tx => await tx.RunAsync(Text, Parameters)
        //                .ConfigureAwait(false)).ConfigureAwait(false) :
        //                    await session.ReadTransactionAsync(async tx => await tx.RunAsync(Text)
        //                        .ConfigureAwait(false)).ConfigureAwait(false);
        //    }

        //    return cursor;
        //}
    }
}
