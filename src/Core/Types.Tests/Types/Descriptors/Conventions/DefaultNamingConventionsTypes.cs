using Xunit;

namespace HotChocolate.Types.Descriptors
{
    public class DefaultNamingConventionsTests
    {
        [InlineData(true)]
        [InlineData(1)]
        [InlineData("abc")]
        [InlineData(Foo.Bar)]
        [Theory]
        public void GetEnumValueDescription_NoDescription(object value)
        {
            // arrange
            var namingConventions = new DefaultNamingConventions();

            // act
            string result = namingConventions.GetEnumValueDescription(value);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void GetEnumValueDescription_XmlDescription()
        {
            // arrange
            var namingConventions = new DefaultNamingConventions();

            // act
            string result = namingConventions.GetEnumValueDescription(
                EnumWithDocEnum.Value1);

            // assert
            Assert.Equal("Value1 Documentation", result);
        }

        [Fact]
        public void GetEnumValueDescription_AttributeDescription()
        {
            // arrange
            var namingConventions = new DefaultNamingConventions();

            // act
            string result = namingConventions.GetEnumValueDescription(Foo.Baz);

            // assert
            Assert.Equal("Baz Desc", result);
        }

        private enum Foo
        {
            Bar,

            [GraphQLDescription("Baz Desc")]
            Baz
        }
    }
}
