using System;

#nullable enable

namespace HotChocolate.Types.Errors
{
    internal delegate object? CreateError(Exception exception);
}
