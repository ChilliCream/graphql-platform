using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInputType
        : IType
    {
        Type NativeType { get; }
        bool IsInstanceOfType(IValueNode literal);
        object ParseLiteral(IValueNode literal);
    }
}
