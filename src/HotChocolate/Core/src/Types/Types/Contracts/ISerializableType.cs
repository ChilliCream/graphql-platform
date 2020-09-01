using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types
{
    public interface ISerializableType : IType
    {
        object? Serialize(object? runtimeValue);

        object? Deserialize(object? resultValue);

        bool TryDeserialize(object? resultValue, out object? runtimeValue);
    }

    public interface IParsableType : IType
    {
        bool IsInstanceOfType(IValueNode valueSyntax);

        bool IsInstanceOfType(object? runtimeValue);

        object? ParseLiteral(IValueNode valueSyntax);

        IValueNode? ParseValue(object runtimeValue);
    }
}
