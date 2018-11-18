using HotChocolate;
using Xunit;

namespace Types.Tests
{
    public class NameStringTests
    {
        [Fact]
        public void ImplicitCast_String_NameString()
        {
            // arrange
            const string name = "Foo";

            // act
            NameString nameString = name;

            // assert
            Assert.Equal(name, nameString.Value);
        }

        [Fact]
        public void ImplicitCast_NameString_String()
        {
            // arrange
            var nameString = new NameString("Foo");

            // act
            string name = nameString;

            // assert
            Assert.Equal(nameString.Value, name);
        }

         [Fact]
        public void ImplicitCast_Void()
        {
            // arrange
            NameStringTests nameString = null;

            // act

            // assert
        }
    }
}
