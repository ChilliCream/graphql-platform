using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class StringOperationInputTests
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
                        .Argument("test", a => a.Type<StringOperationInput>()))
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
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class FooFilterType : FilterInputType
        {
            protected override void Configure(IFilterInputTypeDescriptor descriptor)
            {
                descriptor.Field("string").Type<StringOperationInput>();
            }
        }

        public class Foo
        {
            public string String { get; set; } = "";

            public string? StringNullable { get; set; }
        }
    }
}
