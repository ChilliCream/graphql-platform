using System;

namespace HotChocolate.Language.Utilities
{
    public static class ThrowHelper
    {
        public static void NodeKindIsNotSupported(NodeKind kind) =>
            throw new NotSupportedException($"The node kind {kind} is not supported.");

    }
}
