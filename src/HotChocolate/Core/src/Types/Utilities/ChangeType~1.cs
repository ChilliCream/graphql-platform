using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Utilities
{
    public delegate TTo ChangeType<TFrom, TTo>([MaybeNull]TFrom source);
}
