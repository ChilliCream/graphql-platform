using System;
using HotChocolate.ApolloFederation.Extensions;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.ApolloFederation
{
    public class EntityTypeTest
    {
        [Fact]
        public void TestEntityTypeSchemaFirst()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddDocumentFromString(
                    @"
                    type Query {
                        user(a: Int!): User
                    }

                    type Review @key(fields: ""id"") {
                        id: Int!
                        author: User
                    }

                    type User @key(fields: ""id"") {
                        id: Int!
                        reviews: [Review!]!
                    }
                "
                )
                .Use(next => context => default)
                .Create();

            // assert
            EntityType entityType = schema.GetType<EntityType>("_Entity");
            Assert.Collection(entityType.Types.Values,
                t => Assert.Equal("Review", t.Name),
                t => Assert.Equal("User", t.Name));
        }

        [Fact]
        public void TestEntityTypeCodeFirstClassKeyAttribute()
        {
            // arrange
            // act
            var schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query>()
                .Create();

            // assert
            EntityType entityType = schema.GetType<EntityType>("_Entity");
            Assert.Collection(entityType.Types.Values,
                t => Assert.Equal("UserWithClassAttribute", t.Name),
                t => Assert.Equal("Review", t.Name));
        }
    }

    public class Query
    {
        public UserWithClassAttribute GetUser(int id) => default;
    }

    [Key("Id IdCode")]
    public class UserWithClassAttribute
    {
        public int Id { get; set; }
        public string IdCode { get; set; }
        public Review[] Reviews { get; set; }
    }

    [Key("Id")]
    public class Review
    {
        public int Id { get; set; }
        public UserWithClassAttribute Author { get; set; }
    }
}
