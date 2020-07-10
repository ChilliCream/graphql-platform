using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types
{
    public static class Scalars
    {
        private static readonly Dictionary<Type, ClrTypeReference> _lookup =
            new Dictionary<Type, ClrTypeReference>
            {
                { typeof(string), TypeReference.Create(
                    typeof(StringType), TypeContext.None) },
                { typeof(bool), TypeReference.Create(
                    typeof(BooleanType), TypeContext.None) },
                { typeof(byte), TypeReference.Create(
                    typeof(ByteType), TypeContext.None) },
                { typeof(short), TypeReference.Create(
                    typeof(ShortType), TypeContext.None) },
                { typeof(int), TypeReference.Create(
                    typeof(IntType), TypeContext.None) },
                { typeof(long), TypeReference.Create(
                    typeof(LongType), TypeContext.None) },

                { typeof(float), TypeReference.Create(
                    typeof(FloatType), TypeContext.None) },
                { typeof(double), TypeReference.Create(
                    typeof(FloatType), TypeContext.None) },
                { typeof(decimal), TypeReference.Create(
                    typeof(DecimalType), TypeContext.None) },

                { typeof(Uri), TypeReference.Create(
                    typeof(UrlType), TypeContext.None) },
                { typeof(Guid), TypeReference.Create(
                    typeof(UuidType), TypeContext.None) },
                { typeof(DateTime), TypeReference.Create(
                    typeof(DateTimeType), TypeContext.None) },
                { typeof(DateTimeOffset), TypeReference.Create(
                    typeof(DateTimeType), TypeContext.None) },
                { typeof(MultiplierPathString), TypeReference.Create(
                    typeof(MultiplierPathType), TypeContext.None) },
                { typeof(byte[]), TypeReference.Create(
                    typeof(ByteArrayType), TypeContext.None) },
                { typeof(NameString), TypeReference.Create(
                    typeof(NameType), TypeContext.None) },
                { typeof(TimeSpan), TypeReference.Create(
                    typeof(TimeSpanType), TypeContext.None) },
            };

        private static readonly Dictionary<NameString, ClrTypeReference> _nameLookup =
           new Dictionary<NameString, ClrTypeReference>
           {
                { ScalarNames.String, TypeReference.Create(
                    typeof(StringType), TypeContext.None) },
                { ScalarNames.ID, TypeReference.Create(
                    typeof(IdType), TypeContext.None) },
                { ScalarNames.Boolean, TypeReference.Create(
                    typeof(BooleanType), TypeContext.None) },
                { ScalarNames.Byte, TypeReference.Create(
                    typeof(ByteType), TypeContext.None) },
                { ScalarNames.Short, TypeReference.Create(
                    typeof(ShortType), TypeContext.None) },
                { ScalarNames.Int, TypeReference.Create(
                    typeof(IntType), TypeContext.None) },
                { ScalarNames.Long, TypeReference.Create(
                    typeof(LongType), TypeContext.None) },

                { ScalarNames.Float, TypeReference.Create(
                    typeof(FloatType), TypeContext.None) },
                { ScalarNames.Decimal, TypeReference.Create(
                    typeof(DecimalType), TypeContext.None) },

                { ScalarNames.Url, TypeReference.Create(
                    typeof(UrlType), TypeContext.None) },
                { ScalarNames.Uuid, TypeReference.Create(
                    typeof(UuidType), TypeContext.None) },
                { ScalarNames.DateTime, TypeReference.Create(
                    typeof(DateTimeType), TypeContext.None) },
                { ScalarNames.Date, TypeReference.Create(
                    typeof(DateType), TypeContext.None) },
                { ScalarNames.MultiplierPath, TypeReference.Create(
                    typeof(MultiplierPathType), TypeContext.None) },
                { ScalarNames.Name, TypeReference.Create(
                    typeof(NameType), TypeContext.None) },
                { ScalarNames.PaginationAmount, TypeReference.Create(
                    typeof(PaginationAmountType), TypeContext.None) },
                { ScalarNames.ByteArray, TypeReference.Create(
                    typeof(ByteArrayType), TypeContext.None) },
           };

        private static readonly Dictionary<Type, ValueKind> _scalarKinds =
            new Dictionary<Type, ValueKind>
            {
                { typeof(string), ValueKind.String },
                { typeof(long), ValueKind.Integer },
                { typeof(int), ValueKind.Integer },
                { typeof(short), ValueKind.Integer },
                { typeof(long?), ValueKind.Integer },
                { typeof(int?), ValueKind.Integer },
                { typeof(short?), ValueKind.Integer },
                { typeof(ulong), ValueKind.Integer },
                { typeof(uint), ValueKind.Integer },
                { typeof(ushort), ValueKind.Integer },
                { typeof(ulong?), ValueKind.Integer },
                { typeof(uint?), ValueKind.Integer },
                { typeof(ushort?), ValueKind.Integer },
                { typeof(byte), ValueKind.Integer },
                { typeof(byte?), ValueKind.Integer },
                { typeof(float), ValueKind.Float },
                { typeof(double), ValueKind.Float },
                { typeof(decimal), ValueKind.Float },
                { typeof(float?), ValueKind.Float },
                { typeof(double?), ValueKind.Float },
                { typeof(decimal?), ValueKind.Float },
                { typeof(bool), ValueKind.Float },
                { typeof(bool?), ValueKind.Float }
            };

        internal static bool TryGetScalar(
            Type clrType,
            out ClrTypeReference schemaType)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            return _lookup.TryGetValue(clrType, out schemaType);
        }

        internal static bool TryGetScalar(
            NameString typeName,
            out ClrTypeReference schemaType)
        {
            return _nameLookup.TryGetValue(
                typeName.EnsureNotEmpty(nameof(typeName)),
                out schemaType);
        }

        public static bool IsBuiltIn(NameString typeName)
        {
            return typeName.HasValue && _nameLookup.ContainsKey(typeName);
        }

        public static bool TryGetKind(object value, out ValueKind kind)
        {
            if (value is null)
            {
                kind = ValueKind.Null;
                return true;
            }

            Type valueType = value.GetType();

            if (valueType.IsEnum)
            {
                kind = ValueKind.Enum;
                return true;
            }

            if (_scalarKinds.TryGetValue(valueType, out kind))
            {
                return true;
            }

            if (value is IDictionary)
            {
                kind = ValueKind.Object;
                return true;
            }

            if (value is ICollection)
            {
                kind = ValueKind.List;
                return true;
            }

            kind = ValueKind.Unknown;
            return false;
        }
    }
}
