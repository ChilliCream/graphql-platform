using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class ObjectFilterInputTypeTests
        : TypeTestBase
    {
        //
        [Fact]
        public void Create_ObjectFilter_Discover_Everything()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<Foo>());

            // assert
            schema.ToString().MatchSnapshot();
        }
        [Fact]
        public void Create_ObjectFilter_BindExplicitly()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<Foo>(descriptor => descriptor.BindExplicitly()
                    .Filter(x => x.BarNested)
                    .AllowObject(x =>
                        x.BindExplicitly()
                        .Filter(y => y.Baz).BindExplicitly().AllowEquals()
                    )));

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class Foo
        {
            public string Bar { get; set; }
            public Bar BarNested { get; set; }
        }

        public class Bar
        {
            public string Baz { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.BindExplicitly()
                    .Filter(x => x.BarNested)
                    .AllowObject(x =>
                        x.BindExplicitly()
                        .Filter(y => y.Baz)
                    );

            }
        }
    }
}
