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
            var descriptor = new ObjectTypeDescriptor<Foo>();

            // act
            IObjectTypeDescriptor<Foo> desc = descriptor;
            desc.Field(t => t.A);
            desc.Field(t => t.B).Ignore();

            // assert
            Assert.Collection(
                descriptor.CreateDescription().Fields.Select(t => t.Name),
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
