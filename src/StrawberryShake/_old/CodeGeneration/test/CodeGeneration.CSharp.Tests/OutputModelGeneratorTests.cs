using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OutputModelGeneratorTests
    {
        [Fact]
        public async Task GenerateModel()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new OutputModelGenerator();

            var descriptor = new OutputModelDescriptor(
                "Test",
                "Demo",
                new[] { "ITest" },
                new[] { new OutputFieldDescriptor("Foo", "foo", "Bar") });

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public void CanHandle()
        {
            // arrange
            var generator = new OutputModelGenerator();

            var descriptor = new OutputModelDescriptor(
                "Test",
                "Demo",
                new[] { "ITest" },
                new[] { new OutputFieldDescriptor("Foo", "foo", "Bar") });

            // act
            var canHandle = generator.CanHandle(descriptor);

            // assert
            Assert.True(canHandle);
        }
    }
}
