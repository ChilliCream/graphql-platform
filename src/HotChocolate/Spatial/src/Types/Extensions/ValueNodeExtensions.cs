using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Spatial
{
    public static class ValueNodeExtensions
    {
        public static void EnsureObjectValueNode(
            this IValueNode valueSyntax,
            out ObjectValueNode objectValueSyntax) =>
            objectValueSyntax = valueSyntax as ObjectValueNode ??
                throw new InvalidOperationException();
    }
}
