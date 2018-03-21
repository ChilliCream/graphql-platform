using System;

namespace Prometheus.Types
{
    public class ListType
        : IOutputType
        , IInputType
    {
        public ListType(IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type is ListType)
            {
                throw new ArgumentException(
                    "It is not possible to put a list type into list type.",
                    nameof(type));
            }

            if(type is NonNullType)
        }

        public IType ElementType { get; }
    }
}