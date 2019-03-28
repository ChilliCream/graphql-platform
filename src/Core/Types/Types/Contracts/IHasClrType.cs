using System;

namespace HotChocolate.Types
{
    public interface IHasClrType
    {
        Type ClrType { get; }
    }
}
