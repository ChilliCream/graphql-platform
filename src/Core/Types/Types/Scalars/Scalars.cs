using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types
{
    internal static class Scalars
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

        public static bool TryGetScalar(
            Type clrType,
            out IClrTypeReference schemaType)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            return _lookup.TryGetValue(clrType, out schemaType);
        }
    }
}
