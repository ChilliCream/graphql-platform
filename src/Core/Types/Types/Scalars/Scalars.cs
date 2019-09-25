using System;
using System.Collections.Generic;
using System.Numerics;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types
{
    public static class Scalars
    {
        private static readonly Dictionary<Type, IClrTypeReference> _lookup =
            new Dictionary<Type, IClrTypeReference>
            {
                { typeof(string), new ClrTypeReference(
                    typeof(StringType), TypeContext.None) },
                { typeof(bool), new ClrTypeReference(
                    typeof(BooleanType), TypeContext.None) },
                { typeof(byte), new ClrTypeReference(
                    typeof(ByteType), TypeContext.None) },
                { typeof(short), new ClrTypeReference(
                    typeof(ShortType), TypeContext.None) },
                { typeof(int), new ClrTypeReference(
                    typeof(IntType), TypeContext.None) },
                { typeof(long), new ClrTypeReference(
                    typeof(LongType), TypeContext.None) },

                { typeof(float), new ClrTypeReference(
                    typeof(FloatType), TypeContext.None) },
                { typeof(double), new ClrTypeReference(
                    typeof(FloatType), TypeContext.None) },
                { typeof(decimal), new ClrTypeReference(
                    typeof(DecimalType), TypeContext.None) },

                { typeof(Uri), new ClrTypeReference(
                    typeof(UrlType), TypeContext.None) },
                { typeof(Guid), new ClrTypeReference(
                    typeof(UuidType), TypeContext.None) },
                { typeof(DateTime), new ClrTypeReference(
                    typeof(DateTimeType), TypeContext.None) },
                { typeof(DateTimeOffset), new ClrTypeReference(
                    typeof(DateTimeType), TypeContext.None) },
                { typeof(MultiplierPathString), new ClrTypeReference(
                    typeof(MultiplierPathType), TypeContext.None) },
                { typeof(NameString), new ClrTypeReference(
                    typeof(NameType), TypeContext.None) },
            };

        private static readonly Dictionary<NameString, IClrTypeReference> _nameLookup =
           new Dictionary<NameString, IClrTypeReference>
           {
                { "String", new ClrTypeReference(
                    typeof(StringType), TypeContext.None) },
                { "ID", new ClrTypeReference(
                    typeof(IdType), TypeContext.None) },
                { "Boolean", new ClrTypeReference(
                    typeof(BooleanType), TypeContext.None) },
                { "Byte", new ClrTypeReference(
                    typeof(ByteType), TypeContext.None) },
                { "Short", new ClrTypeReference(
                    typeof(ShortType), TypeContext.None) },
                { "Int", new ClrTypeReference(
                    typeof(IntType), TypeContext.None) },
                { "Long", new ClrTypeReference(
                    typeof(LongType), TypeContext.None) },

                { "Float", new ClrTypeReference(
                    typeof(FloatType), TypeContext.None) },
                { "Decimal", new ClrTypeReference(
                    typeof(DecimalType), TypeContext.None) },

                { "Url", new ClrTypeReference(
                    typeof(UrlType), TypeContext.None) },
                { "Uuid", new ClrTypeReference(
                    typeof(UuidType), TypeContext.None) },
                { "DateTime", new ClrTypeReference(
                    typeof(DateTimeType), TypeContext.None) },
                { "Date", new ClrTypeReference(
                    typeof(DateType), TypeContext.None) },
                { "MultiplierPath", new ClrTypeReference(
                    typeof(MultiplierPathType), TypeContext.None) },
                { "Name", new ClrTypeReference(
                    typeof(NameType), TypeContext.None) },
                { "PaginationAmount", new ClrTypeReference(
                    typeof(PaginationAmountType), TypeContext.None) },
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
            out IClrTypeReference schemaType)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            return _lookup.TryGetValue(clrType, out schemaType);
        }

        internal static bool TryGetScalar(
            NameString typeName,
            out IClrTypeReference schemaType)
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

            return _scalarKinds.TryGetValue(valueType, out kind);
        }
    }
}
