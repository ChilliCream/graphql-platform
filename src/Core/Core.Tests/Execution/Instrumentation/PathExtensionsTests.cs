using System.Collections.Generic;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Execution.Instrumentation
{
    public class PathExtensionsTests
    {
        [Fact]
        public void ToFieldPathArray()
        {
            // arrange
            Path path = Path
                .New("hero")
                .Append("friends")
                .Append(0)
                .Append("name");

            // act
            IReadOnlyCollection<object> result = path.ToFieldPathArray();

            // assert
            result.Snapshot();
        }
    }
}
