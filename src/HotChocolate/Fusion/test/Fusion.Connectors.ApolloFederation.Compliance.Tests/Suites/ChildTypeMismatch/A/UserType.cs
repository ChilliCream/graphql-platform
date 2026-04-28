using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ChildTypeMismatch.A;

/// <summary>
/// Descriptor for the <c>User</c> type in the <c>a</c> subgraph.
/// No <c>@key</c> is declared; <c>id</c> is marked <c>@shareable</c>
/// so both subgraphs can expose it.
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Field(u => u.Id).Shareable().Type<NonNullType<IdType>>();
    }
}
