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

        private static readonly Dictionary<Type, ScalarKind> _scalarKinds =
            new Dictionary<Type, ScalarKind>
            {
                { typeof(string), ScalarKind.String },
                { typeof(long), ScalarKind.Integer },
                { typeof(int), ScalarKind.Integer },
                { typeof(short), ScalarKind.Integer },
                { typeof(ulong), ScalarKind.Integer },
                { typeof(uint), ScalarKind.Integer },
                { typeof(ushort), ScalarKind.Integer },
                { typeof(byte), ScalarKind.Integer },
                { typeof(float), ScalarKind.Float },
                { typeof(double), ScalarKind.Float },
                { typeof(decimal), ScalarKind.Float },
                { typeof(bool), ScalarKind.Float }
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

        public static bool TryGetKind(object value, out ScalarKind kind)
        {
            if (value is null)
            {
                kind = ScalarKind.Null;
                return true;
            }

            Type valueType = value.GetType();

            if (valueType.IsEnum)
            {
                kind = ScalarKind.Enum;
                return true;
            }

            return _scalarKinds.TryGetValue(valueType, out kind);
        }
    }

    public enum ScalarKind
    {
        String,
        Integer,
        Float,
        Boolean,
        Enum,
        Null
    }
}
