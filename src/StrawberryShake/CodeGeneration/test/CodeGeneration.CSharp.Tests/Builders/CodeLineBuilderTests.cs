using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class CodeLineBuilderTests
    {
        [Fact]
        public async Task CreateCodeLine()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await CodeLineBuilder.New()
                .SetLine("abcd;")
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }
    }
}
