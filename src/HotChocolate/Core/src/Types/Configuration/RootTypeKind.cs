#nullable enable

namespace HotChocolate.Configuration;

internal enum RootTypeKind
{
    Query = 0,
    Mutation = 1,
    Subscription = 2,
    None = 3
}
