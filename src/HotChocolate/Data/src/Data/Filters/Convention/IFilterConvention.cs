using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public interface IFilterConvention
        : IConvention
    {
        NameString GetOperationName(
            IDescriptorContext context,
            int operation);

        NameString GetOperationDescription(
            IDescriptorContext context,
            int operation);

        NameString GetFieldName(
            IDescriptorContext context,
            MemberInfo member);

        NameString GetFieldDescription(
            IDescriptorContext context,
            MemberInfo member);

        ITypeReference GetFieldType(
            IDescriptorContext context,
            MemberInfo member);

        NameString GetTypeName(
            IDescriptorContext context,
            Type entityType);

        NameString GetTypeDescription(
            IDescriptorContext context,
            Type entityType);

        IEnumerable<Action<IFilterInputTypeDescriptor>> GetExtensions(
            TypeReference reference);
    }
}
