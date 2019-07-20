using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class ObjectFilterInputTypeTests
        : TypeTestBase
    {
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

                descriptor.Filter(x => x.BarNested).AllowObject(x => x.Filter(y => y.Baz));

                descriptor.Filter(x => x.BarNested).AllowObject<FilterInputType<Bar>>();
            }
        }
    }
}
