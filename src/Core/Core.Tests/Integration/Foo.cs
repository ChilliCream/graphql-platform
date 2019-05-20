using System;
using HotChocolate.Execution;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Integration.Foo
{

    public class FooTests
    {
        [Fact]
        public void TestMe()
        {
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .Create();

            schema.MakeExecutable().Execute(
                QueryRequestBuilder.New()
                    .SetQuery("{ foo { bar { baz }}}")
                    .Create())
                .MatchSnapshot();
        }
    }

    public class QueryType
        : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field<QueryType>(t => t.Foo()).Type<FooType>();
        }

        public object Foo()
        {
            return new Object();
        }
    }

    public class FooType
        : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field<FooType>(t => t.Bar()).Type<BarType>();
        }

        public Bar Bar()
        {
            return new Bar();
        }
    }

    public class BarType
        : ObjectType<Bar>
    {
        protected override void Configure(IObjectTypeDescriptor<Bar> descriptor)
        {

        }
    }

    public class Bar
    {
        public string Baz { get; } = "Bar";
    }
}
