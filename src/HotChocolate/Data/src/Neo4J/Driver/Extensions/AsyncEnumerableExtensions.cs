using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public static class AsyncEnumerableExtensions
    {
        public static IAsyncEnumerable<TReturn> Map<TReturn>(
            this IAsyncEnumerable<IRecord> records)
        {
            return records.Select(record => record.Map<TReturn>());
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IAsyncEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TValue16, TReturn>(
            this IAsyncEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TValue16, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }
    }

}
