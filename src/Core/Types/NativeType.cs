using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class NativeType<T>
        : IOutputType
        , IInputType
    {
        public TypeKind Kind => throw new NotImplementedException();

        Type IInputType.NativeType => throw new NotImplementedException();

        public bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public object ParseLiteral(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public IValueNode ParseValue(object value)
        {
            throw new NotImplementedException();
        }
    }
}
