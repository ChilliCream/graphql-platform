using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterConvention : IFilterConvention
    {
        public NameString GetFieldDescription(IDescriptorContext context, MemberInfo member)
        {
            throw new NotImplementedException();
        }

        public NameString GetFieldName(IDescriptorContext context, MemberInfo member)
        {
            throw new NotImplementedException();
        }

        public ITypeReference GetFieldType(IDescriptorContext context, MemberInfo member)
        {
            throw new NotImplementedException();
        }

        public NameString GetOperationDescription(IDescriptorContext context, int operation)
        {
            throw new NotImplementedException();
        }

        public NameString GetOperationName(IDescriptorContext context, int operation)
        {
            throw new NotImplementedException();
        }

        public ITypeReference GetOperationType(IDescriptorContext context, int operation)
        {
            throw new NotImplementedException();
        }

        public NameString GetTypeDescription(IDescriptorContext context, Type entityType)
        {
            throw new NotImplementedException();
        }

        public NameString GetTypeName(IDescriptorContext context, Type entityType)
        {
            throw new NotImplementedException();
        }

        public bool TryCreateImplicitFilter(PropertyInfo property, [NotNullWhen(true)] out InputFieldDefinition? definition)
        {
            throw new NotImplementedException();
        }
    }
}
