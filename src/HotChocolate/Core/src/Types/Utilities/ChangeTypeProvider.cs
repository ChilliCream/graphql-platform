using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Utilities;

public delegate bool ChangeTypeProvider(
    Type source,
    Type target,
    [NotNullWhen(true)] out ChangeType? converter);
