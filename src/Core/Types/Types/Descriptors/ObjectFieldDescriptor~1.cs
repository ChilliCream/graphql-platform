using System;
using System.Reflection;

namespace HotChocolate.Types.Descriptors
{
    public class ObjectFieldDescriptor<T>
        : ObjectFieldDescriptor
        , IObjectFieldDescriptor<T>
    {
        internal protected ObjectFieldDescriptor(
            IDescriptorContext context,
            NameString fieldName)
            : base(context, fieldName)
        {
        }

        internal protected ObjectFieldDescriptor(
            IDescriptorContext context,
            MemberInfo member)
            : base(context, member)
        {
        }

        internal protected ObjectFieldDescriptor(
            IDescriptorContext context,
            MemberInfo member,
            Type resolverType)
            : base(context, member, resolverType)
        {
        }
    }
}
