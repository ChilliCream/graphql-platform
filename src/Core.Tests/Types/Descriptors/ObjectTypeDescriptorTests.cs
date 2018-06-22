using System.Linq;
using Xunit;

namespace HotChocolate.Types
{
    public class ObjectTypeDescriptorTests
    {
        [Fact]
        public void IgnoreFields()
        {
            // arrange
            ObjectTypeDescriptor<Foo> descriptor =
                new ObjectTypeDescriptor<Foo>();

            // act
            IObjectTypeDescriptor<Foo> desc = (IObjectTypeDescriptor<Foo>)descriptor;
            desc.Field(t => t.A);
            desc.Field(t => t.B).Ignore();

            // assert
            Assert.Collection(
                descriptor.GetFieldDescriptors().Select(t => t.Name),
                t => Assert.Equal("a", t),
                t => Assert.Equal("c", t));
        }

        public class Foo
        {
            public string A { get; set; }
            public string B { get; set; }
            public string C { get; set; }
        }
    }
}
