using System.Collections.Generic;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution.ValueConverters
{
    public class ListValueConverterTests
    {
        [Fact]
        public void CanConvert_StringListType_True()
        {
            // arrange
            ListValueConverter converter = new ListValueConverter();
            ListType type = new ListType(new StringType());

            // act
            bool result = converter.CanConvert(type);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void CanConvert_StringType_True()
        {
            // arrange
            ListValueConverter converter = new ListValueConverter();
            StringType type = new StringType();

            // act
            bool result = converter.CanConvert(type);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void Convert_StringArray_StringList()
        {
            // arrange
            ListValueConverter converter = new ListValueConverter();
            ListType type = new ListType(new StringType());
            string[] input = new string[] { "1", "2" };

            // act
            bool result = converter.TryConvert(
                typeof(string[]), typeof(List<string>),
                input, out object convertedValue);

            // assert
            Assert.True(result);
            Assert.IsType<List<string>>(convertedValue);
            Assert.Collection((List<string>)convertedValue,
                t => Assert.Equal("1", t),
                t => Assert.Equal("2", t));
        }

        [Fact]
        public void Convert_Null_Null()
        {
            // arrange
            ListValueConverter converter = new ListValueConverter();
            ListType type = new ListType(new StringType());

            // act
            bool result = converter.TryConvert(
                typeof(string[]), typeof(List<string>),
                null, out object convertedValue);

            // assert
            Assert.True(result);
            Assert.Null(convertedValue);
        }
    }
}
