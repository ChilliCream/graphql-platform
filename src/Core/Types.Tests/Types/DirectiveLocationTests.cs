using System;
using System.Linq;
using Xunit;

namespace HotChocolate.Types
{
    public class DirectiveLocationTests
    {
        // flag values must be set correct 
        [Fact]
        public void FlagsCorrect()
        {
            Enum.GetValues(typeof(DirectiveLocation))
                .Cast<DirectiveLocation>()
                .Aggregate(0, (acc, loc) =>
                {
                    var v = acc == 0 ? 1 : acc * 2;
                    Assert.Equal(v, (int)loc);
                    return v;
                });
        } 
    }
}
