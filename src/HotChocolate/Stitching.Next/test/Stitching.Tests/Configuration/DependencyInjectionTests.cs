using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Configuration;

public class DependencyInjectionTests
{
    [Fact]
    public async Task Federation_BasicSchema()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQLGateway()
            .AddRemoteSchema(
                @"schema @schema(name: ""Accounts"") {
                    query: Query
                }

                type Query {
                    users: [User!]
                    userById(id: ID! @is(a: ""User.id"")): User
                }

                type User {
                    id: ID!
                    name: String!
                    username: String!
                    birthdate: DateTime!
                }")
            .AddRemoteSchema(
                @"schema @schema(name: ""Reviews"") {
                    query: Query
                }

                type Query {
                    reviews: [Review!]!
                    reviewsById(ids: [ID!] @is(a: ""Review.id"")): [Review!]
                    # reviewsByAuthor(authorId: ID! @is(a: ""User.id"")): [Review!] @internal
                    # reviewsByProduct(upc: ID! @is(a: ""Product.upc"")): [Review!] @internal
                    productById(upc: ID! @is(a: ""Product.upc"")): Product
                    userById(id: ID! @is(a: ""User.id"")): User
                }

                # reviewsById(ids: [ID!] @is(a: ""User.id"")): [Review!]
                # reviewsById all is must point to review and must return Review
                type Review {
                    id: ID!
                    user: User!
                    product: Product!
                    body: String!
                }
                
                type User {
                    id: ID!
                    name: String!
                    reviews: [Review!]
                }
                
                type Product {
                    upc: ID
                    reviews: [Review!]
                }")
            .Builder
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }
}