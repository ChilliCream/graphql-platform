using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace HotChocolate.AspNetCore.Grpc
{
    /// <summary>
    /// Google Protobuf WellKnown Types convertors and helpers
    /// </summary>
    public static class GoogleProtobufWellKnownTypesConvertors
    {
        /// <summary>
        /// Convert Google.Protobuf.WellKnownTypes.Struct to IDictionary
        /// </summary>
        /// <param name="struct"></param>
        /// <returns></returns>
        public static IDictionary<string, object?> ToDictionary(Struct @struct)
        {
            var result = @struct.Fields
                .GetEnumerator()
                .ToIEnumerable()
                .ToDictionary(x => x.Key, x => x.Value?.ToObject());
            return result;
        }

        /// <summary>
        /// Convert IDictionary to Google.Protobuf.WellKnownTypes.Struct
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static Struct ToStruct(IDictionary<string, object?> dictionary)
        {
            var entries = dictionary.ToDictionary(x => x.Key, x => x.Value?.ToValue());
            var fields = new MapField<string, Value?>
            {
                entries
            };
            var result = new Struct
            {
                Fields =
                {
                    fields
                }
            };

            return result;
        }

        /// <summary>
        /// Convert IDictionary to Google.Protobuf.WellKnownTypes.Struct
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static Struct ToStruct(IEnumerable<KeyValuePair<string, object?>> enumerable)
            => ToStruct(enumerable.ToDictionary(x => x.Key, x => x.Value));

        /// <summary>
        /// Convert System.Object to Google.Protobuf.WellKnownTypes.Value
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        public static Value? ToValue(object @object) =>
            @object switch
            {
                bool val => Value.ForBool(val),
                double val => Value.ForNumber(val),
                string val => Value.ForString(val),
                IEnumerable<object> val => Value.ForList(val.Select(x => x.ToValue()).ToArray()),
                // TODO: Throw exception...need find problem
                IDictionary<string, object> val => val.ToStruct().ToValue(),
                null => null,
                _ => throw new ArgumentException(message: "Object is not a recognized Value", paramName: nameof(@object))
            };

        /// <summary>
        /// Convert Google.Protobuf.WellKnownTypes.Value to System.Object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object? ToObject(Value value) =>
            value.KindCase switch
            {
                Value.KindOneofCase.StructValue => (value.StructValue.ToDictionary() as object),
                Value.KindOneofCase.BoolValue => value.BoolValue,
                Value.KindOneofCase.NumberValue => value.NumberValue,
                Value.KindOneofCase.StringValue => value.StringValue,
                Value.KindOneofCase.ListValue => value.ListValue.Values.Select(x => x.ToObject()).ToList(),
                Value.KindOneofCase.None => null,
                Value.KindOneofCase.NullValue => null,
                _ => null
            };
    }
}
