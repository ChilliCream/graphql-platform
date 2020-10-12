using System;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.ApolloFederation
{
    public class ReferenceResolverAttributeTests
    {
        [Fact]
        public async void InClassRefResolver_PureCodeFirst()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query>()
                .Create();

            // act
            ObjectType type = schema.GetType<ObjectType>(nameof(InClassRefResolver));

            // assert
            var result = await ResolveRef(schema, type);
            Assert.Equal(
                nameof(InClassRefResolver),
                Assert.IsType<InClassRefResolver>(result).Id);
        }

        [Fact]
        public async void ExternalRefResolver_PureCodeFirst()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query>()
                .Create();

            // act
            ObjectType type =
                schema.GetType<ObjectType>(nameof(ExternalRefResolver));

            // assert
            var result = await ResolveRef(schema, type);

            Assert.Equal(
                nameof(ExternalRefResolver),
                Assert.IsType<ExternalRefResolver>(result).Id);
        }

        [Fact]
        public async void ExternalRefResolver_RenamedMethod_PureCodeFirst()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query>()
                .Create();

            // act
            ObjectType type =
                schema.GetType<ObjectType>(nameof(ExternalRefResolverRenamedMethod));

            // assert
            var result = await ResolveRef(schema, type);
            Assert.Equal(
                nameof(ExternalRefResolverRenamedMethod),
                Assert.IsType<ExternalRefResolver>(result).Id);
        }

        [Fact]
        public void InClassRefResolver_RenamedMethod_InvalidName_PureCodeFirst()
        {
            // arrange
            Action schemaCreation = () =>
            {
                ISchema schema = SchemaBuilder.New()
                    .AddApolloFederation()
                    .AddQueryType<Query_InClass_Invalid>()
                    .Create();
            };

            // act
            // assert
            Assert.Throws<SchemaException>(schemaCreation);
        }

        [Fact]
        public void ExternalRefResolver_RenamedMethod_InvalidName_PureCodeFirst()
        {
            // arrange
            Action schemaCreation = () =>
            {
                ISchema schema = SchemaBuilder.New()
                    .AddApolloFederation()
                    .AddQueryType<Query_ExternalClass_Invalid>()
                    .Create();
            };

            // act
            // assert
            Assert.Throws<SchemaException>(schemaCreation);
        }

        private ValueTask<object?> ResolveRef(ISchema schema, ObjectType type)
        {
            var inClassResolverContextObject =
                type.ContextData[WellKnownContextData.EntityResolver];
            Assert.NotNull(inClassResolverContextObject);
            var inClassResolverDelegate =
                Assert.IsType<FieldResolverDelegate>(inClassResolverContextObject);
            var context = new MockResolverContext(schema);

            context.SetLocalValue("data", new ObjectValueNode());
            return inClassResolverDelegate.Invoke(context);
        }

        public class Query_InClass_Invalid
        {
            public InvalidInClassRefResolver InvalidInClassRefResolver { get; set; }
        }

        public class Query_ExternalClass_Invalid
        {
            public ExternalRefResolver_Invalid ExternalRefResolver_Invalid { get; set; }
        }

        [ReferenceResolver(EntityResolver = "non-existing-method")]
        public class InvalidInClassRefResolver
        {
            [Key]
            public string Id { get; set; }
        }

        [ReferenceResolver(
            EntityResolverType = typeof(InvalidExternalRefResolver),
            EntityResolver = "non-existing-method")]
        public class ExternalRefResolver_Invalid
        {
            [Key]
            public string Id { get; set; }
        }

        public class InvalidExternalRefResolver
        {
            [Key]
            public string Id { get; set; }
        }


        public class Query
        {
            public InClassRefResolver InClassRefResolver { get; set; }
            public ExternalRefResolver ExternalRefResolver { get; set; }
            public ExternalRefResolverRenamedMethod ExternalRefResolverRenamedMethod { get; set; }
        }

        [ReferenceResolver(EntityResolver = nameof(GetAsync))]
        public class InClassRefResolver
        {
            [Key]
            public string Id { get; set; }

            public async Task<InClassRefResolver> GetAsync([LocalState] ObjectValueNode data)
            {
                return new InClassRefResolver(){Id = nameof(InClassRefResolver)};
            }
        }

        [ReferenceResolver(EntityResolverType = typeof(ExternalReferenceResolver))]
        public class ExternalRefResolver
        {
            [Key]
            public string Id { get; set; }
        }

        [ReferenceResolver(
            EntityResolverType = typeof(ExternalReferenceResolverRenamedMethod),
            EntityResolver = nameof(ExternalReferenceResolverRenamedMethod.SomeRenamedMethod))]
        public class ExternalRefResolverRenamedMethod
        {
            [Key]
            public string Id { get; set; }
        }

        public static class ExternalReferenceResolverRenamedMethod
        {
            public static async Task<ExternalRefResolver> SomeRenamedMethod(
                [LocalState] ObjectValueNode data)
            {
                return new ExternalRefResolver(){Id = nameof(ExternalRefResolverRenamedMethod)};
            }
        }

        public static class ExternalReferenceResolver
        {
            public static async Task<ExternalRefResolver> GetExternalReferenceResolverAsync(
                [LocalState] ObjectValueNode data)
            {
                return new ExternalRefResolver(){Id = nameof(ExternalRefResolver)};
            }
        }

    }
}
