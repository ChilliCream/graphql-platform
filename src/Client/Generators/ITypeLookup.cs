using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public interface ITypeLookup
    {
        string GetTypeName(FieldNode field, IType fieldType, bool readOnly);

        TypeInfo GetTypeInfo(FieldNode field, IType fieldType, bool readOnly);

        string GetTypeName(IType fieldType, string typeName, bool readOnly);
    }
}
