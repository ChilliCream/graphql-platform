using System;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class SchemaBuilderExtensionsTypeTests
    {
        [Fact]
        public void AddQueryTypeDesc_ConfigureIsNull_ArgumentNullException()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddQueryType(builder, (Action<IObjectTypeDescriptor>)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddQueryTypeDesc_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilderExtensions
                .AddQueryType(null, t => { });

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddQueryTypeDesc_ConfigureQueryType_SchemaIsCreated()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            SchemaBuilderExtensions.AddQueryType(builder,
                t => t.Name("Foo").Field("bar").Resolver("result"));

            // assert
            builder.Create().ToString().MatchSnapshot();
        }

        [Fact]
        public void AddMutationTypeDesc_ConfigureIsNull_ArgumentNullException()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddMutationType(builder, (Action<IObjectTypeDescriptor>)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddMutationTypeDesc_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilderExtensions
                .AddMutationType(null, t => { });

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddMutationTypeDesc_ConfigureQueryType_SchemaIsCreated()
        {
            // arrange
            var builder = new SchemaBuilder();
            builder.AddQueryType<QueryType>();

            // act
            SchemaBuilderExtensions.AddMutationType(builder,
                t => t.Name("Foo").Field("bar").Resolver("result"));

            // assert
            builder.Create().ToString().MatchSnapshot();
        }

        [Fact]
        public void AddSubscriptionTypeDesc_ConfigureIsNull_ArgNullException()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddMutationType(builder, (Action<IObjectTypeDescriptor>)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddSubscriptionTypeDesc_BuilderIsNull_ArgNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilderExtensions
                .AddMutationType(null, t => { });

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddSubscriptionTypeDesc_ConfigureQueryType_SchemaIsCreated()
        {
            // arrange
            var builder = new SchemaBuilder();
            builder.AddQueryType<QueryType>();

            // act
            SchemaBuilderExtensions.AddSubscriptionType(builder,
                t => t.Name("Foo").Field("bar").Resolver("result"));

            // assert
            builder.Create().ToString().MatchSnapshot();
        }

        [Fact]
        public void AddQueryTypeDescT_ConfigureIsNull_ArgumentNullException()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddQueryType(builder,
                    (Action<IObjectTypeDescriptor<Foo>>)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddQueryTypeDescT_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilderExtensions
                .AddQueryType<Foo>(null, t => { });

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddQueryTypeDescT_ConfigureQueryType_SchemaIsCreated()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            SchemaBuilderExtensions.AddQueryType<Foo>(builder,
                t => t.Name("Foo").Field(f => f.Bar).Resolver("result"));

            // assert
            builder.Create().ToString().MatchSnapshot();
        }

        [Fact]
        public void AddMutationTypeDescT_ConfigureIsNull_ArgumentNullException()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddMutationType(builder,
                    (Action<IObjectTypeDescriptor<Foo>>)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddMutationTypeDescT_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilderExtensions
                .AddMutationType<Foo>(null, t => { });

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddMutationTypeDescT_ConfigureQueryType_SchemaIsCreated()
        {
            // arrange
            var builder = new SchemaBuilder();
            builder.AddQueryType<QueryType>();

            // act
            SchemaBuilderExtensions.AddMutationType<Foo>(builder,
                t => t.Name("Foo").Field(f => f.Bar).Resolver("result"));

            // assert
            builder.Create().ToString().MatchSnapshot();
        }

        [Fact]
        public void AddSubscriptionTypeDescT_ConfigureIsNull_ArgNullException()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddMutationType(builder,
                    (Action<IObjectTypeDescriptor<Foo>>)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddSubscriptionTypeDescT_BuilderIsNull_ArgNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilderExtensions
                .AddMutationType<Foo>(null, t => { });

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddSubscriptionTypeDescT_ConfQueryType_SchemaIsCreated()
        {
            // arrange
            var builder = new SchemaBuilder();
            builder.AddQueryType<QueryType>();

            // act
            SchemaBuilderExtensions.AddSubscriptionType<Foo>(builder,
                t => t.Name("Foo").Field(f => f.Bar).Resolver("result"));

            // assert
            builder.Create().ToString().MatchSnapshot();
        }

        public class QueryType
           : ObjectType
        {
            protected override void Configure(
                IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("foo").Type<StringType>().Resolver("bar");
            }
        }

        public class MutationType
            : ObjectType
        {
            protected override void Configure(
                IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Mutation");
                descriptor.Field("bar").Type<IntType>().Resolver(123);
            }
        }

        public class SubscriptionType
            : ObjectType
        {
            protected override void Configure(
                IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Subscription");
                descriptor.Field("onFoo").Type<IntType>().Resolver(123);
            }
        }

        public class FooType
            : ObjectType<Foo>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Bar).Type<NonNullType<BarType>>();
            }
        }

        public class BarType
            : ObjectType<Bar>
        {
        }

        public class Foo
        {
            public Bar Bar { get; }
        }

        public class Bar
        {
            public string Baz { get; }
        }

        [GraphQLResolverOf(typeof(QueryType))]
        public class QueryResolverOnType
        {
            public string Baz { get; }
        }

        [GraphQLResolverOf("Query")]
        public class QueryResolverOnName
        {
            public string Baz { get; }
        }

        public class MySchema
            : Schema
        {
            protected override void Configure(ISchemaTypeDescriptor descriptor)
            {
                descriptor.Description("Description");
            }
        }

        public class MyEnumType
            : EnumType
        { }
    }
}
