using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInputType
        : IType
    {
        bool IsInstanceOfType(IValueNode literal);
        object ParseLiteral(IValueNode literal, Type targetType);
    }
}
