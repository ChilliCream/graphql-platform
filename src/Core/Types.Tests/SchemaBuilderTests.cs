using HotChocolate.Types;
using Xunit;
using Snapshooter.Xunit;
using HotChocolate.Language;

namespace HotChocolate
{
    public class SchemaBuilderTests
    {
        [Fact]
        public void Create_SingleType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddRootType(typeof(FooType), OperationType.Query)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddQueryType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(typeof(FooType))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddQueryType_Generic()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddQueryType_GenericClr()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Foo>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void InferQueryType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<QueryType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddMutationType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddMutationType(typeof(FooType))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddMutationType_Generic()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddMutationType<FooType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddMutationType_GenericClr()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddMutationType<Foo>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void InferMutationType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<QueryType>()
                .AddType<MutationType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddSubscriptionType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddSubscriptionType(typeof(FooType))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddSubscriptionType_Generic()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddSubscriptionType<FooType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddSubscriptionType_GenericClr()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddSubscriptionType<Foo>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void InferSubscriptionType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<QueryType>()
                .AddType<SubscriptionType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        // TODO : Add Missing Query Type test
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
    }
}
