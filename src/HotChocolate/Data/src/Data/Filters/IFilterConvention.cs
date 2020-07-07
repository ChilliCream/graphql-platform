using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public interface IFilterConvention
    {
        NameString GetOperationName(int fieldKind, int operationKind);

        NameString GetOperationDescription(int fieldKind, int operationKind);

        ITypeReference GetOperationType(int fieldKind, int operationKind);

        NameString GetFieldName(int fieldKind);

        NameString GetFieldDescription(int fieldKind);

        ITypeReference GetFieldType(int fieldKind);

        NameString GetMethodName(int fieldKind);

        NameString GetMethodDescription(int fieldKind);

        ITypeReference GetMethodType(int fieldKind);

        NameString GetTypeName(Type entityType);

        NameString GetTypeDescription(Type entityType);

        bool TryCreateImplicitFilter(
            PropertyInfo property,
            [NotNullWhen(true)] out InputFieldDefinition? definition);
    }
}
