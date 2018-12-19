using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInputType
        : IType
        , IHasClrType
    {
        bool IsInstanceOfType(IValueNode literal);

        object ParseLiteral(IValueNode literal);

        IValueNode ParseValue(object value);
    }

    public interface IHasClrType
    {
        Type ClrType { get; }
    }
}
