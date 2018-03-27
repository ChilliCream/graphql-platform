using System;

namespace Prometheus.Types
{
    public class NonNullType
    {
        public NonNullType(IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!(type is INullableType))
            {
                throw new ArgumentException(
                    "The inner type of non-null type must be a nullable type",
                    nameof(type));
            }

            Type = type;
        }

        public IType Type { get; }
    }
}