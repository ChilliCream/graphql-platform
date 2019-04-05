using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    // this is just a marker type for the fluent code-first api.
    public sealed class ListType<T>
        : IOutputType
        , IInputType
        where T : IType
    {
        private ListType()
        {
        }

        public Type ClrType => throw new NotSupportedException();

        public TypeKind Kind => throw new NotSupportedException();

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
