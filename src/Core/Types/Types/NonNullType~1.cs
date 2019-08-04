using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    // this is just a marker type for the fluent code-first api.
    public sealed class NonNullType<T>
        : IOutputType
        , IInputType
        where T : IType
    {
        private NonNullType()
        {
        }

        Type IHasClrType.ClrType => throw new NotSupportedException();

        TypeKind IType.Kind => throw new NotSupportedException();

        object ISerializableType.Deserialize(object serialized)
        {
            throw new NotSupportedException();
        }

        bool IInputType.IsInstanceOfType(IValueNode literal)
        {
            throw new NotSupportedException();
        }

        bool IInputType.IsInstanceOfType(object value)
        {
            throw new NotSupportedException();
        }

        object IInputType.ParseLiteral(IValueNode literal)
        {
            throw new NotSupportedException();
        }

        IValueNode IInputType.ParseValue(object value)
        {
            throw new NotSupportedException();
        }

        object ISerializableType.Serialize(object value)
        {
            throw new NotSupportedException();
        }

        bool ISerializableType.TryDeserialize(
            object serialized, out object value)
        {
            throw new NotSupportedException();
        }
    }
}
