using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class FluentWrapperType
        : IOutputType
        , IInputType
    {
        protected FluentWrapperType() { }

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
