using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.ApolloFederation
{
    public class EntitiesResolverTests
    {
        [Fact]
        public async void TestResolveViaForeignServiceType()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query>()
                .Create();

            // act
            var context = new MockResolverContext(schema);
            var representations = new List<Representation>()
            {
                new Representation(){Typename = "ForeignType", Data = new ObjectValueNode(
                    new ObjectFieldNode("Id", "1"),
                    new ObjectFieldNode("SomeExternalField", "someExternalField")
                    )}
            };

            // assert
            var result = await EntitiesResolver._Entities(schema, representations, context);
            var obj = Assert.IsType<ForeignType>(result[0]);
            Assert.Equal("1", obj.Id);
            Assert.Equal("someExternalField", obj.SomeExternalField);
            Assert.Equal("IntenalValue", obj.InternalField);
        }

        [Fact]
        public async void TestResolveViaForeignServiceType_MixedTypes()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query>()
                .Create();

            // act
            var context = new MockResolverContext(schema);
            var representations = new List<Representation>()
            {
                new Representation(){Typename = "MixedFieldTypes", Data = new ObjectValueNode(
                    new ObjectFieldNode("Id", "1"),
                    new ObjectFieldNode("IntField", 25)
                )}
            };

            // assert
            var result = await EntitiesResolver._Entities(schema, representations, context);
            var obj = Assert.IsType<MixedFieldTypes>(result[0]);
            Assert.Equal("1", obj.Id);
            Assert.Equal(25, obj.IntField);
            Assert.Equal("IntenalValue", obj.InternalField);
        }

        [Fact]
        public async void TestResolveViaEntityResolver()
        {
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query>()
                .Create();

            // act
            var context = new MockResolverContext(schema);
            var representations = new List<Representation>()
            {
                new Representation(){Typename = "TypeWithReferenceResolver", Data = new ObjectValueNode(
                    new ObjectFieldNode("Id", "1")
                )}
            };

            // assert
            var result = await EntitiesResolver._Entities(schema, representations, context);
            var obj = Assert.IsType<TypeWithReferenceResolver>(result[0]);
            Assert.Equal("1", obj.Id);
            Assert.Equal("SomeField", obj.SomeField);
        }

        [Fact]
        public async void TestResolveViaEntityResolver_NoTypeFound()
        {
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query>()
                .Create();

            // act
            var context = new MockResolverContext(schema);
            var representations = new List<Representation>()
            {
                new Representation(){Typename = "NonExistingTypeName", Data = new ObjectValueNode()}
            };

            // assert
            Func<Task> shouldThrow = () => EntitiesResolver._Entities(schema, representations, context);
            await Assert.ThrowsAsync<SchemaException>(shouldThrow);
        }

        [Fact]
        public async void TestResolveViaEntityResolver_NoResolverFound()
        {
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query>()
                .Create();

            // act
            var context = new MockResolverContext(schema);
            var representations = new List<Representation>()
            {
                new Representation(){Typename = "TypeWithoutRefResolver", Data = new ObjectValueNode()}
            };

            // assert
            Func<Task> shouldThrow = () => EntitiesResolver._Entities(schema, representations, context);
            await Assert.ThrowsAsync<SchemaException>(shouldThrow);
        }

        public class Query
        {
            public ForeignType ForeignType { get; set; }
            public TypeWithReferenceResolver TypeWithReferenceResolver { get; set; }
            public TypeWithoutRefResolver TypeWithoutRefResolver { get; set; }
            public MixedFieldTypes MixedFieldTypes { get; set; }
        }

        public class TypeWithoutRefResolver
        {
            public string Id { get; set; }
        }

        [ReferenceResolver(EntityResolver = nameof(Get))]
        public class TypeWithReferenceResolver
        {
            public string Id { get; set; }
            public string SomeField { get; set; }

            public static TypeWithReferenceResolver Get([LocalState] ObjectValueNode data)
            {
                return new TypeWithReferenceResolver(){Id = "1", SomeField = "SomeField"};
            }
        }

        [ForeignServiceTypeExtension]
        public class ForeignType
        {
            [Key][External]
            public string Id { get; set; }

            [External]
            public string SomeExternalField { get; set; }

            public string InternalField { get; set; } = "IntenalValue";
        }

        [ForeignServiceTypeExtension]
        public class MixedFieldTypes
        {
            [Key][External]
            public string Id { get; set; }

            [External]
            public int IntField { get; set; }

            public string InternalField { get; set; } = "IntenalValue";
        }
    }
}
