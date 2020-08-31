using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class EnumOperationInputTests
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
                        .Argument("test", a => a.Type<EnumOperationInput<FooBar>>()))
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
                .TryAddConvention<IFilterConvention>(
                    (sp) => new FilterConvention(x => x.UseMock()))
                .UseFiltering()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class FooFilterType : FilterInputType
        {
            protected override void Configure(IFilterInputTypeDescriptor descriptor)
            {
                descriptor.Field("comparable").Type<EnumOperationInput<FooBar>>();
            }
        }

        public class Foo
        {
            public FooBar FooBar { get; set; }

            public FooBar? FooBarNullable { get; set; }
        }

        public enum FooBar
        {
            Foo,
            Bar
        }
    }
}