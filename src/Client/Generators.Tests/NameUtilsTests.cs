using Xunit;

namespace StrawberryShake.Generators
{
    public class NameUtilsTests
    {
        [InlineData("abc_def", "AbcDef")]
        [InlineData("abc__def", "AbcDef")]
        [InlineData("__abc__def", "AbcDef")]
        [InlineData("__abc__def__", "AbcDef")]
        [Theory]
        public void Convert_Property_Snail_Name(string input, string expected)
        {
            // act
            string output = NameUtils.GetPropertyName(input);

            // assert
            Assert.Equal(expected, output);
        }

        [InlineData("d", "ID")]
        [InlineData("ITest", "ITest")]
        [InlineData("abc_def", "IAbcDef")]
        [InlineData("abc__def", "IAbcDef")]
        [InlineData("__abc__def", "IAbcDef")]
        [InlineData("__abc__def__", "IAbcDef")]
        [Theory]
        public void Convert_Interface_Snail_Name(string input, string expected)
        {
            // act
            string output = NameUtils.GetInterfaceName(input);

            // assert
            Assert.Equal(expected, output);
        }
    }
}
