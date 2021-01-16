using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public static class RecordExtensions
    {
        public static IEnumerable<TReturn> Map<TReturn>(
            this IEnumerable<IRecord> records)
        {
            return records.Select(record => record.Map<TReturn>());
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8,
            TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8,
            TValue9, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8,
            TValue9, TValue10, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TReturn>
                mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8,
            TValue9, TValue10, TValue11, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11,
                TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8,
            TValue9, TValue10, TValue11, TValue12, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11,
                TValue12, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8,
            TValue9, TValue10, TValue11, TValue12, TValue13, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11,
                TValue12, TValue13, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8,
            TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11,
                TValue12, TValue13, TValue14, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8,
            TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11,
                TValue12, TValue13, TValue14, TValue15, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static IEnumerable<TReturn> Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8,
            TValue9, TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TValue16, TReturn>(
            this IEnumerable<IRecord> records,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11,
                TValue12, TValue13, TValue14, TValue15, TValue16, TReturn> mapFunc)
        {
            return records.Select(record => record.Map(mapFunc));
        }

        public static TReturn Map<TReturn>(
            this IRecord record)
        {
            return ValueMapper.MapValue<TReturn>(record[0]);
        }

        public static TReturn Map<TValue1, TValue2, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TValue4, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TValue4, TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]),
                ValueMapper.MapValue<TValue4>(record[3]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TValue4, TValue5, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]),
                ValueMapper.MapValue<TValue4>(record[3]),
                ValueMapper.MapValue<TValue5>(record[4]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]),
                ValueMapper.MapValue<TValue4>(record[3]),
                ValueMapper.MapValue<TValue5>(record[4]),
                ValueMapper.MapValue<TValue6>(record[5]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]),
                ValueMapper.MapValue<TValue4>(record[3]),
                ValueMapper.MapValue<TValue5>(record[4]),
                ValueMapper.MapValue<TValue6>(record[5]),
                ValueMapper.MapValue<TValue7>(record[6]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]),
                ValueMapper.MapValue<TValue4>(record[3]),
                ValueMapper.MapValue<TValue5>(record[4]),
                ValueMapper.MapValue<TValue6>(record[5]),
                ValueMapper.MapValue<TValue7>(record[6]),
                ValueMapper.MapValue<TValue8>(record[7]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9,
            TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]),
                ValueMapper.MapValue<TValue4>(record[3]),
                ValueMapper.MapValue<TValue5>(record[4]),
                ValueMapper.MapValue<TValue6>(record[5]),
                ValueMapper.MapValue<TValue7>(record[6]),
                ValueMapper.MapValue<TValue8>(record[7]),
                ValueMapper.MapValue<TValue9>(record[8]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9,
            TValue10, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TReturn>
                map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]),
                ValueMapper.MapValue<TValue4>(record[3]),
                ValueMapper.MapValue<TValue5>(record[4]),
                ValueMapper.MapValue<TValue6>(record[5]),
                ValueMapper.MapValue<TValue7>(record[6]),
                ValueMapper.MapValue<TValue8>(record[7]),
                ValueMapper.MapValue<TValue9>(record[8]),
                ValueMapper.MapValue<TValue10>(record[9]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9,
            TValue10, TValue11, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11,
                TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]),
                ValueMapper.MapValue<TValue4>(record[3]),
                ValueMapper.MapValue<TValue5>(record[4]),
                ValueMapper.MapValue<TValue6>(record[5]),
                ValueMapper.MapValue<TValue7>(record[6]),
                ValueMapper.MapValue<TValue8>(record[7]),
                ValueMapper.MapValue<TValue9>(record[8]),
                ValueMapper.MapValue<TValue10>(record[9]),
                ValueMapper.MapValue<TValue11>(record[10]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9,
            TValue10, TValue11, TValue12, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11,
                TValue12, TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]),
                ValueMapper.MapValue<TValue4>(record[3]),
                ValueMapper.MapValue<TValue5>(record[4]),
                ValueMapper.MapValue<TValue6>(record[5]),
                ValueMapper.MapValue<TValue7>(record[6]),
                ValueMapper.MapValue<TValue8>(record[7]),
                ValueMapper.MapValue<TValue9>(record[8]),
                ValueMapper.MapValue<TValue10>(record[9]),
                ValueMapper.MapValue<TValue11>(record[10]),
                ValueMapper.MapValue<TValue12>(record[11]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9,
            TValue10, TValue11, TValue12, TValue13, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11,
                TValue12, TValue13, TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]),
                ValueMapper.MapValue<TValue4>(record[3]),
                ValueMapper.MapValue<TValue5>(record[4]),
                ValueMapper.MapValue<TValue6>(record[5]),
                ValueMapper.MapValue<TValue7>(record[6]),
                ValueMapper.MapValue<TValue8>(record[7]),
                ValueMapper.MapValue<TValue9>(record[8]),
                ValueMapper.MapValue<TValue10>(record[9]),
                ValueMapper.MapValue<TValue11>(record[10]),
                ValueMapper.MapValue<TValue12>(record[11]),
                ValueMapper.MapValue<TValue13>(record[12]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9,
            TValue10, TValue11, TValue12, TValue13, TValue14, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11,
                TValue12, TValue13, TValue14, TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]),
                ValueMapper.MapValue<TValue4>(record[3]),
                ValueMapper.MapValue<TValue5>(record[4]),
                ValueMapper.MapValue<TValue6>(record[5]),
                ValueMapper.MapValue<TValue7>(record[6]),
                ValueMapper.MapValue<TValue8>(record[7]),
                ValueMapper.MapValue<TValue9>(record[8]),
                ValueMapper.MapValue<TValue10>(record[9]),
                ValueMapper.MapValue<TValue11>(record[10]),
                ValueMapper.MapValue<TValue12>(record[11]),
                ValueMapper.MapValue<TValue13>(record[12]),
                ValueMapper.MapValue<TValue14>(record[13]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9,
            TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11,
                TValue12, TValue13, TValue14, TValue15, TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]),
                ValueMapper.MapValue<TValue4>(record[3]),
                ValueMapper.MapValue<TValue5>(record[4]),
                ValueMapper.MapValue<TValue6>(record[5]),
                ValueMapper.MapValue<TValue7>(record[6]),
                ValueMapper.MapValue<TValue8>(record[7]),
                ValueMapper.MapValue<TValue9>(record[8]),
                ValueMapper.MapValue<TValue10>(record[9]),
                ValueMapper.MapValue<TValue11>(record[10]),
                ValueMapper.MapValue<TValue12>(record[11]),
                ValueMapper.MapValue<TValue13>(record[12]),
                ValueMapper.MapValue<TValue14>(record[13]),
                ValueMapper.MapValue<TValue15>(record[14]));
        }

        public static TReturn Map<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9,
            TValue10, TValue11, TValue12, TValue13, TValue14, TValue15, TValue16, TReturn>(
            this IRecord record,
            Func<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8, TValue9, TValue10, TValue11,
                TValue12, TValue13, TValue14, TValue15, TValue16, TReturn> map)
        {
            return map(
                ValueMapper.MapValue<TValue1>(record[0]),
                ValueMapper.MapValue<TValue2>(record[1]),
                ValueMapper.MapValue<TValue3>(record[2]),
                ValueMapper.MapValue<TValue4>(record[3]),
                ValueMapper.MapValue<TValue5>(record[4]),
                ValueMapper.MapValue<TValue6>(record[5]),
                ValueMapper.MapValue<TValue7>(record[6]),
                ValueMapper.MapValue<TValue8>(record[7]),
                ValueMapper.MapValue<TValue9>(record[8]),
                ValueMapper.MapValue<TValue10>(record[9]),
                ValueMapper.MapValue<TValue11>(record[10]),
                ValueMapper.MapValue<TValue12>(record[11]),
                ValueMapper.MapValue<TValue13>(record[12]),
                ValueMapper.MapValue<TValue14>(record[13]),
                ValueMapper.MapValue<TValue15>(record[14]),
                ValueMapper.MapValue<TValue16>(record[15]));
        }
    }
}
