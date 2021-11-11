using System.Collections.Generic;
using Xunit;

namespace StrawberryShake.Tools.Tests
{
    public class CustomHeaderHelperTests
    {
        [Fact]
        public void ParseHeadersArgument_Test()
        {
            // arrange
            var arguments = new List<string?>
            {
                "myheader=myvalue",
                "header-name=a-value-one,a-value-two",
                "not-a-key-value-header",
                null,
                "headername=mybase64=.encodedvalue=.",
            };

            // act
            Dictionary<string, IEnumerable<string>> headers = CustomHeaderHelper.ParseHeadersArgument(arguments);

            // assert
            Assert.Equal(3, headers.Count);
            Assert.Equal(new [] {"myvalue"}, headers["myheader"]);
            Assert.Equal(new [] {"a-value-one", "a-value-two"}, headers["header-name"]);
            Assert.False(headers.ContainsKey("not-a-key-value-header"));
            Assert.Equal(new [] {"mybase64=.encodedvalue=."}, headers["headername"]);
        }
    }
}
