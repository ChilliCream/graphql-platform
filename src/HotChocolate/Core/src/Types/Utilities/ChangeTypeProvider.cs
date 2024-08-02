using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Utilities;

public delegate bool ChangeTypeProvider(
    Type source,
    Type target,
    [NotNullWhen(true)] out ChangeType? converter);
