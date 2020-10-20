using Xunit;

namespace HotChocolate.Types.Descriptors
{
    public class DefaultNamingConventionsTests
    {
        [InlineData("Foo", "FOO")]
        [InlineData("FooBar", "FOO_BAR")]
        [InlineData("FooBarBaz", "FOO_BAR_BAZ")]
        [InlineData("FOO_BAR_BAZ", "FOO_BAR_BAZ")]
        [InlineData("FOOBAR", "FOOBAR")]
        [InlineData("F", "F")]
        [InlineData("f", "F")]
        [Theory]
        public void GetEnumName(string runtimeName, string expectedSchemaName)
        {
            // arrange
            var namingConventions = new DefaultNamingConventions();

            // act
            NameString schemaName = namingConventions.GetEnumValueName(runtimeName);

            // assert
            Assert.Equal(expectedSchemaName, schemaName.Value);
        }

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
            var result = namingConventions.GetEnumValueDescription(value);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void GetEnumValueDescription_XmlDescription()
        {
            // arrange
            var namingConventions = new DefaultNamingConventions();

            // act
            var result = namingConventions.GetEnumValueDescription(EnumWithDocEnum.Value1);

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
