using System.Threading.Tasks;
using HotChocolate.Types;
using Xunit;
using Snapshooter.Xunit;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using System;
using HotChocolate.Execution;

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

        [Fact]
        public void Use_MiddlewareNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .Use((FieldMiddleware)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Use_MiddlewareDelegate()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocument(sp =>
                    Parser.Default.Parse("type Query { a: String }"))
                .Use(next => context =>
                {
                    context.Result = "foo";
                    return Task.CompletedTask;
                })
                .Create();


            // assert
            schema.MakeExecutable().Execute("{ a }").MatchSnapshot();
        }

        [Fact]
        public void AddDocument_DocumentIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddDocument((LoadSchemaDocument)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddDocument_MultipleDocuments_Are_Merged()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocument(sp =>
                    Parser.Default.Parse("type Query { a: Foo }"))
                .AddDocument(sp =>
                    Parser.Default.Parse("type Foo { a: String }"))
                .Use(next => context =>
                {
                    context.Result = "foo";
                    return Task.CompletedTask;
                })
                .Create();


            // assert
            schema.MakeExecutable().Execute("{ a { a } }").MatchSnapshot();
        }

        [Fact]
        public void AddType_TypeIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddType((Type)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddType_TypeIsResolverTypeByName_QueryContainsBazField()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddType(typeof(QueryResolverOnType))
                .Create();

            // assert
            ObjectType queryType = schema.GetType<ObjectType>("Query");
            Assert.True(queryType.Fields.ContainsField("baz"));
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
    }
}
