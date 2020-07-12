using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterConvention : IFilterConvention
    {
        internal static readonly IFilterConvention Default = new FilterConvention();

        public NameString GetFieldDescription(IDescriptorContext context, MemberInfo member)
            => context.Naming.GetMemberDescription(member, MemberKind.InputObjectField);

        public NameString GetFieldName(IDescriptorContext context, MemberInfo member)
            => context.Naming.GetMemberName(member, MemberKind.InputObjectField);

        public ITypeReference GetFieldType(IDescriptorContext context, MemberInfo member)
            => context.Inspector.GetInputReturnType(member);

        public NameString GetOperationDescription(IDescriptorContext context, int operation)
            => "desc" + operation;

        public NameString GetOperationName(IDescriptorContext context, int operation)
            => "operation" + operation;

        public NameString GetTypeDescription(IDescriptorContext context, Type entityType)
            => context.Naming.GetTypeDescription(entityType, TypeKind.InputObject);

        public NameString GetTypeName(IDescriptorContext context, Type entityType)
            => context.Naming.GetTypeName(entityType, TypeKind.InputObject);

        public bool TryCreateImplicitFilter(
            PropertyInfo property,
            [NotNullWhen(true)] out InputFieldDefinition? definition)
        {
            throw new NotImplementedException();
        }
    }
}
