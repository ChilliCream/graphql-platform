using System;

namespace Prometheus.Types
{
    public class ListType
        : IOutputType
        , IInputType
    {
        public ListType(IType elementType)
        {
            if (elementType == null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            if (elementType.IsListType())
            {
                throw new ArgumentException(
                    "It is not possible to put a list type into list type.",
                    nameof(elementType));
            }

            ElementType = elementType;
        }

        public IType ElementType { get; }
    }
}