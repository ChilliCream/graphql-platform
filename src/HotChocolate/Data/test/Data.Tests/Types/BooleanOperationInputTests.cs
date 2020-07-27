using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class BooleanOperationInputTests
    {
        [Fact]
        public void Create_OperationType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(
                    t => t
                        .Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("foo")
                        .Argument("test", a => a.Type<BooleanOperationInput>()))
                .UseFiltering()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Implicit_Operation()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(
                    t => t
                        .Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("foo")
                        .Argument("test", a => a.Type<FilterInputType<Foo>>()))
                .UseFiltering()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Explicit_Operation()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(
                    t => t
                        .Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("foo")
                        .Argument("test", a => a.Type<FooFilterType>()))
                .UseFiltering()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class FooFilterType : FilterInputType
        {
            protected override void Configure(IFilterInputTypeDescriptor descriptor)
            {
                descriptor.Field("boolean").Type<BooleanOperationInput>();
            }
        }

        public class Foo
        {
            public bool Boolean { get; set; }

            public bool? BooleanNullable { get; set; }
        }
    }
}