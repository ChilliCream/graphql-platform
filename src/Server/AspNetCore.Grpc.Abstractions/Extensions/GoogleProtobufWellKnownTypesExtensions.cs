using System;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;

namespace HotChocolate.AspNetCore.Grpc
{
    /// <summary>
    /// Google Protobuf WellKnown Types convertors and helpers
    /// </summary>
    public static class GoogleProtobufWellKnownTypesExtensions
    {
        /// <summary>
        /// Convert Google.Protobuf.WellKnownTypes.Struct to IDictionary
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IDictionary<string, object?> ToDictionary(this Struct value)
            => GoogleProtobufWellKnownTypesConvertors.ToDictionary(value);

        /// <summary>
        /// Convert Google.Protobuf.WellKnownTypes.Value to System.Object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object? ToObject(this Value value)
            => GoogleProtobufWellKnownTypesConvertors.ToObject(value);

        /// <summary>
        /// Convert System.Object to Google.Protobuf.WellKnownTypes.Value
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        public static Value? ToValue(this object @object)
            => GoogleProtobufWellKnownTypesConvertors.ToValue(@object);

        /// <summary>
        /// Convert IDictionary to Google.Protobuf.WellKnownTypes.Struct
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static Struct ToStruct(this IDictionary<string, object?> dictionary)
            => GoogleProtobufWellKnownTypesConvertors.ToStruct(dictionary);

        /// <summary>
        /// Convert IDictionary to Google.Protobuf.WellKnownTypes.Struct
        /// </summary>
        /// <param name="readOnlyDictionary"></param>
        /// <returns></returns>
        public static Struct ToStruct(this IReadOnlyDictionary<string, object?> readOnlyDictionary)
            => GoogleProtobufWellKnownTypesConvertors.ToStruct(readOnlyDictionary);

        /// <summary>
        /// Convert DateTime? to Google.Protobuf.WellKnownTypes.Timestamp
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static Timestamp? ToTimestamp(this DateTime? dateTime) =>
            dateTime == null ? null : Timestamp.FromDateTime(dateTime.Value);
    }
}
