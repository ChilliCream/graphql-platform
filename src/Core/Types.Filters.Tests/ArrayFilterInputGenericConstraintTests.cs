using System;
using System.Collections.Generic;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class ArrayFilterInputGenericConstraintTests
        : TypeTestBase
    {

        //
        [Theory]
        [MemberData(nameof(GetData)]
        public void Create_ArrayObjectFilter_ShouldMatchSameSnapshotInAllCases(FilterInputType<Foo> input)
        {
            // arrange
            // act
            var schema = CreateSchema(input);

            // assert
            schema.ToString().MatchSnapshot();
        }

        public static IEnumerable<object[]> GetData => new List<FilterInputType<Foo>[]>
           {
            new[] {new FilterInputType<Foo>(x => x.Filter(y => y.IEnumerable).AllowSome()) },
            new[] {new FilterInputType<Foo>(x => x.Filter(y => y.List).AllowSome()) }
           };

        public class Foo
        {
            public List<Bar> List { get; set; }
            public IEnumerable<Bar> IEnumerable { get; set; }
            public List<string> ListString { get; set; }
            public IEnumerable<Bar> IEnumerableString { get; set; }
        }

        public class Bar
        {
            public string Baz { get; set; }
        }


    }
}
