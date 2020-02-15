using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EnumValueSerializerGeneratorTests
    {
        [Fact]
        public async Task Generate()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new EnumValueSerializerGenerator();

            var descriptor = new EnumValueSerializerDescriptor(
                "EpisodeValueSerializer",
                "Episode",
                "global::Demo.Episode",
                new []
                {
                    new EnumElementDescriptor("NewHope", "NEWHOPE"),
                    new EnumElementDescriptor("Empire", "EMPIRE"),
                }
            );

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_With_Value()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new EnumValueSerializerGenerator();

            var descriptor = new EnumValueSerializerDescriptor(
                "EpisodeValueSerializer",
                "Episode",
                "global::Demo.Episode",
                new []
                {
                    new EnumElementDescriptor("NewHope", "NEWHOPE"),
                    new EnumElementDescriptor("Empire", "EMPIRE"),
                }
            );

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public void CanHandle()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new EnumValueSerializerGenerator();

            var descriptor = new EnumValueSerializerDescriptor(
                "EpisodeValueSerializer",
                "Episode",
                "global::Demo.Episode",
                new []
                {
                    new EnumElementDescriptor("NewHope", "NEWHOPE"),
                    new EnumElementDescriptor("Empire", "EMPIRE"),
                }
            );

            // act
            bool result = generator.CanHandle(descriptor);

            // assert
            Assert.True(result);
        }
    }
}
