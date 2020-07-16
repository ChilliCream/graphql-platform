using System;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Utilities
{
    public interface IChangeTypeProvider
    {
        bool TryCreateConverter(
            Type source, 
            Type target, 
            ChangeTypeProvider root,
            [NotNullWhen(true)]out ChangeType? converter);
    }
}
