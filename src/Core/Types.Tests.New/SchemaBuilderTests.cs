using System.Linq;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using Moq;
using Xunit;
using Snapshooter.Xunit;
using Snapshooter;

namespace HotChocolate
{
    public class SchemaBuilderTests
    {
        [Fact]
        public void Create_OneType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(FooType))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }




        public class FooType
            : ObjectType<Foo>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Bar).Type<NonNullType<BarType>>();
            }
        }

        public class BarType
            : ObjectType<Bar>
        {
        }

        public class Foo
        {
            public Bar Bar { get; }
        }

        public class Bar
        {
            public string Baz { get; }
        }
    }
}
