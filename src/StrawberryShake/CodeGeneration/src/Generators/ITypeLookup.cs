using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public interface ITypeLookup
    {
        string GetTypeName(IType fieldType, FieldNode? field, bool readOnly);

        string GetTypeName(IType fieldType, string? typeName, bool readOnly);

        ITypeInfo GetTypeInfo(IType fieldType, bool readOnly);

        string GetLeafClrTypeName(IType type);

        Type GetSerializationType(IType type);
    }
}
