using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class FilterFieldDescriptor
        : InputFieldDescriptor
    {
        public FilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context, property)
        {
        }

        protected virtual ITypeReference GetTypeReference()
        {
            if (Definition.Type is ClrTypeReference type)
            {
                Type innerType = type.Type;
                // TODO: This might not work
                if (type.Type.IsValueType)
                {
                    innerType = typeof(Nullable<>).MakeGenericType(type.Type);
                }
                return new ClrTypeReference(innerType, type.Context, true, true);
            }
            throw new ArgumentException("Definition has no valid Type");
        }
    }
}
