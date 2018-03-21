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

            if (type.IsNonNullType())
            {
                throw new ArgumentException(
                    "A non null type cannot be the inner type of a non null type.",
                    nameof(type));
            }
        }

        public IType Type { get; }
    }
}