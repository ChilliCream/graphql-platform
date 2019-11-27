#nullable enable

namespace HotChocolate.Utilities
{
    internal interface ITypeInfoFactory
    {
        bool TryCreate(IExtendedType type, out TypeInfo? typeInfo);
    }
}
