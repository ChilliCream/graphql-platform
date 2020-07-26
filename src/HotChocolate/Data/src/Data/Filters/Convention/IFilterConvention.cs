using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

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

        NameString GetArgumentName();

        IEnumerable<Action<IFilterInputTypeDescriptor>> GetExtensions(
            ITypeReference reference,
            NameString temporaryName);

        bool TryGetHandler(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition,
            [NotNullWhen(true)] out FilterFieldHandler? handler);

        Task ExecuteAsync<TEntityType>(
            FieldDelegate next,
            IMiddlewareContext context);
    }
}
