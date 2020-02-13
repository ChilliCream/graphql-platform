using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class ParameterBuilderTests
    {
        [Fact]
        public async Task CreateParameter()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await ParameterBuilder.New()
                .SetName("foo")
                .SetType("Bar")
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateParameter_With_Default()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await ParameterBuilder.New()
                .SetName("foo")
                .SetType("Bar")
                .SetDefault()
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateParameter_With_Custom_Default()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await ParameterBuilder.New()
                .SetName("foo")
                .SetType("Bar")
                .SetDefault("1")
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }
    }
}
