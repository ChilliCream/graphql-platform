using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class CodeBlockBuilderTests
    {
        [Fact]
        public async Task CreateCodeBlock()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await CodeBlockBuilder.New()
                .AddCode("abc;")
                .AddCode("def;")
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateCodeBlock_With_CodeLineBuilder()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await CodeBlockBuilder.New()
                .AddCode("abc;")
                .AddCode(CodeLineBuilder.New().SetLine("def;"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateCodeBlock_With_CodeBlockBuilder()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await CodeBlockBuilder.New()
                .AddCode("abc;")
                .AddCode(CodeBlockBuilder.New()
                    .AddCode("def;")
                    .AddCode("ghi;"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }
    }
}
