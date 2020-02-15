using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class InputModelGeneratorTests
    {
        [Fact]
        public async Task GenerateModel()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new InputModelGenerator();

            var descriptor = new InputModelDescriptor(
                "Test",
                "Demo",
                new[] { new InputFieldDescriptor("Foo", "Bar") });

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public void CanHandle()
        {
            // arrange
            var generator = new InputModelGenerator();

            var descriptor = new InputModelDescriptor(
                "Test",
                "Demo",
                new[] { new InputFieldDescriptor("Foo", "Bar") });

            // act
            var canHandle = generator.CanHandle(descriptor);

            // assert
            Assert.True(canHandle);
        }
    }
}
