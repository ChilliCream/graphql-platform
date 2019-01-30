using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class NativeType<T>
        : IOutputType
        , IInputType
    {
        public TypeKind Kind => throw new NotSupportedException();

        public Type ClrType => throw new NotSupportedException();

        public bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotSupportedException();
        }

        public bool IsInstanceOfType(object value)
        {
            throw new NotSupportedException();
        }

        public object ParseLiteral(IValueNode literal)
        {
            throw new NotSupportedException();
        }

        public IValueNode ParseValue(object value)
        {
            throw new NotSupportedException();
        }
    }
}
