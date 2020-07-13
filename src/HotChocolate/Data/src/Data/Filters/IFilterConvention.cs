using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public interface IFilterConvention : IConvention
    {
        NameString GetOperationName(IDescriptorContext context, int operation);

        NameString GetOperationDescription(IDescriptorContext context, int operation);

        ITypeReference GetOperationType(IDescriptorContext context, int operation);

        NameString GetFieldName(IDescriptorContext context, MemberInfo member);

        NameString GetFieldDescription(IDescriptorContext context, MemberInfo member);

        ITypeReference GetFieldType(IDescriptorContext context, MemberInfo member);

        NameString GetTypeName(IDescriptorContext context, Type entityType);

        NameString GetTypeDescription(IDescriptorContext context, Type entityType);

        bool TryCreateImplicitFilter(
            PropertyInfo property,
            [NotNullWhen(true)] out InputFieldDefinition? definition);
    }
}
