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
        NameString GetTypeName(Type entityType);

        NameString GetTypeDescription(Type entityType);

        NameString GetFieldName(MemberInfo member);

        NameString GetFieldDescription(MemberInfo member);

        ITypeReference GetFieldType(MemberInfo member);

        NameString GetOperationName(int operation);

        NameString GetOperationDescription(int operation);

        NameString GetArgumentName();

        void ApplyConfigurations(
            ITypeReference typeReference,
            IFilterInputTypeDescriptor descriptor);

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
