using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.ApolloFederation
{
    public class ServiceTypeTests
    {
        [Fact]
        public async Task TestServiceTypeEmptyQueryTypeSchemaFirst()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddDocumentFromString(@"
                    type Query {

                    }

                    type Address @key(fields: ""matchCode"") {
                        matchCode: String!
                    }
                ")
                .Use(next => context => default)
                .Create();

            // act
            ServiceType entityType = schema.GetType<ServiceType>(WellKnownTypeNames.Service);

            // assert
            object? value = await entityType.Fields[WellKnownFieldNames.Sdl].Resolver(new MockResolverContext(schema));
            value.MatchSnapshot();
        }

        [Fact]
        public async Task TestServiceTypeTypeSchemaFirst()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddDocumentFromString(@"
                    type Query {
                        address: Address!
                    }

                    type Address @key(fields: ""matchCode"") {
                        matchCode: String!
                    }
                ")
                .Use(next => context => default)
                .Create();

            // act
            ServiceType entityType = schema.GetType<ServiceType>(WellKnownTypeNames.Service);

            // assert
            object? value = await entityType.Fields[WellKnownFieldNames.Sdl].Resolver(new MockResolverContext(schema));
            value.MatchSnapshot();
        }


        [Fact]
        public async Task TestServiceTypeEmptyQueryTypePureCodeFirst()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddType<Address>()
                .AddQueryType<EmptyQuery>()
                .Create();

            // act
            ServiceType entityType = schema.GetType<ServiceType>(WellKnownTypeNames.Service);

            // assert
            object? value = await entityType.Fields[WellKnownFieldNames.Sdl].Resolver(new MockResolverContext(schema));
            value.MatchSnapshot();
        }

        [Fact]
        public async Task TestServiceTypeTypePureCodeFirst()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query>()
                .Create();

            // act
            ServiceType entityType = schema.GetType<ServiceType>(WellKnownTypeNames.Service);

            // assert
            object? value = await entityType.Fields[WellKnownFieldNames.Sdl].Resolver(new MockResolverContext(schema));
            value.MatchSnapshot();
        }

        public class EmptyQuery
        {
        }

        public class Query
        {
            public Address GetAddress(int id) => default!;
        }

        public class Address
        {
            [Key]
            public string MatchCode { get; set; }
        }
    }
}
