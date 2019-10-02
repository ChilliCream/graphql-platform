using StrawberryShake.Generators.Utilities;
using Xunit;

namespace StrawberryShake.Generators
{
    public class NameUtilsTests
    {
        /*
        [InlineData("abc_def", "abcDef")]
        [InlineData("abc__def", "abcDef")]
        [InlineData("__abc__def", "abcDef")]
        [InlineData("__abc__def__", "abcDef")]
        [Theory]
        public void Field_Name_Formatter(string input, string expected)
        {
            // act
            string output = NameUtils.GetFieldName(input);

            // assert
            Assert.Equal(expected, output);
        }
         */

        [InlineData("abc_def", "AbcDef")]
        [InlineData("abc__def", "AbcDef")]
        [InlineData("__abc__def", "AbcDef")]
        [InlineData("__abc__def__", "AbcDef")]
        [Theory]
        public void Property_Name_Formatter(string input, string expected)
        {
            // act
            string output = NameUtils.GetPropertyName(input);

            // assert
            Assert.Equal(expected, output);
        }

        [InlineData("abc_def", "AbcDef")]
        [InlineData("abc__def", "AbcDef")]
        [InlineData("__abc__def", "AbcDef")]
        [InlineData("__abc__def__", "AbcDef")]
        [Theory]
        public void Class_Name_Formatter(string input, string expected)
        {
            // act
            string output = NameUtils.GetClassName(input);

            // assert
            Assert.Equal(expected, output);
        }

        [InlineData("d", "ID")]
        [InlineData("ITest", "IITest")]
        [InlineData("abc_def", "IAbcDef")]
        [InlineData("abc__def", "IAbcDef")]
        [InlineData("__abc__def", "IAbcDef")]
        [InlineData("__abc__def__", "IAbcDef")]
        [Theory]
        public void Interface_Name_Formatter(string input, string expected)
        {
            // act
            string output = NameUtils.GetInterfaceName(input);

            // assert
            Assert.Equal(expected, output);
        }
    }
}
