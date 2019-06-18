using System.Linq;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class FilterVisitorTests
        : TypeTestBase
    {
        [Fact]
        public void Create_Implicit_Filters()
        {
            // arrange
            string query = "{ foo(where: { bar: \"a\" AND: [ {bar: \"b\"} {bar: \"c\"} ] OR: [ {bar: \"d\"} {bar: \"e\"} ]  }) }";
            ObjectValueNode value = Utf8GraphQLParser.Parse(query)
                .Definitions.OfType<OperationDefinitionNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().First()
                .Arguments.Select(t => t.Value)
                .OfType<ObjectValueNode>().First();

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new DummyFilter(fooType);
            value.Accept(filter);

            // assert
            Assert.Equal(
                "(Bar = a AND (Bar = b AND Bar = c) AND (Bar = d OR Bar = e))",
                filter.Query);
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
            }
        }
    }
}
