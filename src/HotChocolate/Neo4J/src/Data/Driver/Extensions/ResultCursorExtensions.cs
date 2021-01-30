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

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TValue4, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TValue16, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TValue16, TReturn> mapFunc)
        {
            return await resultCursor.MapAsync(
                record => record.Map(mapFunc)).ConfigureAwait(false);
        }

        public static async Task<List<TReturn>> MapAsync<TReturn>(
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

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TValue4, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
        }

        public static async Task<TReturn> MapSingleAsync<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TValue16, TReturn>(
            this IResultCursor resultCursor,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TValue16, TReturn> mapFunc)
        {
            return (await resultCursor.SingleAsync().ConfigureAwait(false)).Map(mapFunc);
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
