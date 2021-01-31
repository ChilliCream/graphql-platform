using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
public static class ResultCursorExtensions
    {
        public static async Task<List<TReturn>> MapAsync<TReturn>(
            this IResultCursor resultCursor)
        {
            return await resultCursor.MapAsync(
                record => record.Map<TReturn>()).ConfigureAwait(false);
        }

        private static async Task<List<TReturn>> MapAsync<TReturn>(
            this IResultCursor resultCursor,
            Func<IRecord, TReturn> mapFunc)
        {
            var list = new List<TReturn>();
            while (await resultCursor.FetchAsync().ConfigureAwait(false))
            {
                list.Add(mapFunc(resultCursor.Current));
            }
            return list;
        }

        public static async Task<TReturn> MapSingleAsync<TReturn>(
            this IResultCursor resultCursor)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map<TReturn>();
        }

        public static async IAsyncEnumerable<IRecord> AsyncResults(this IResultCursor resultCursor)
        {
            while (await resultCursor.FetchAsync().ConfigureAwait(false))
            {
                yield return resultCursor.Current;
            }
        }
    }
}
