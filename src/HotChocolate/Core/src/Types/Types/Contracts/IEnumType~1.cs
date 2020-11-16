using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Types
{
    public interface IEnumType<T> : IEnumType
    {
        bool TryGetRuntimeValue(
            NameString name,
            [NotNullWhen(true)]out T runtimeValue);
    }
}
