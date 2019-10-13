using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    public class SortInputObjectTypeTests
        : TypeTestBase
    {
        [Fact]
        public void Create_Implicit_Sorting_NoBindInvocation()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new SortInputType<Foo>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Implicit_Sorting()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new SortInputType<Foo>(d => d.BindFieldsImplicitly()));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Implicit_Sorting_WithIgnoredField()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new SortInputType<Foo>(d => d.BindFieldsImplicitly()
                    .SortableObject(f => f.Bar).Ignore()));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Implicit_Sorting_WithRenamedField()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new SortInputType<Foo>(d => d.BindFieldsImplicitly()
                    .SortableObject(f => f.Bar).Name("quux")));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Explicit_Sorting()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new SortInputType<Foo>(d => d
                    .BindFieldsExplicitly()
                    .SortableObject(f => f.Bar)
                ));

            // assert
            schema.ToString().MatchSnapshot();
        }

        private class Foo
        {
            public Bar Bar { get; set; }
        }

        private class Bar
        {
            public string BarProperty { get; set; }
            public Baz Baz { get; set; }
        }
        private class Baz
        {
            public string BarProperty { get; set; }
            public string BazProperty { get; set; }
        }
    }
}
