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
        [Theory]
        [MemberData(nameof(GetData))]
        public void Create_ArrayObjectFilter_ShouldMatchSameSnapshotInAllCases(
            FilterInputType<Foo> input)
        {
            // arrange
            // act
            var schema = CreateSchema(input);

            // assert
            schema.ToString().MatchSnapshot();
        }

        public static IEnumerable<object[]> GetData => new List<FilterInputType<Foo>[]>
        {
            new[] { CreateFilterTypeFor(x => x.List(y => y.IEnumerable)) },
            new[] { CreateFilterTypeFor(x => x.List(y => y.List)) }
        };

        public static FilterInputType<Foo> CreateFilterTypeFor(
            Func<IFilterInputTypeDescriptor<Foo>, IArrayFilterFieldDescriptor<Bar>> expression)
        {
            return new FilterInputType<Foo>(
                x => expression
                    .Invoke(x.BindFieldsExplicitly())
                    .BindExplicitly()
                    .AllowSome()
                    .Name("test"));
        }

        public class Foo
        {
            public List<Bar> List { get; set; }
            public IEnumerable<Bar> IEnumerable { get; set; }
        }

        public class Bar
        {
            public string Baz { get; set; }
        }
    }
}
