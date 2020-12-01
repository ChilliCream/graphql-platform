using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class InputModelSerializerGeneratorTests
    {
        [Fact]
        public async Task Generate()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new InputModelSerializerGenerator();

            var descriptor = new InputModelSerializerDescriptor(
                "ReviewInputSerializer",
                "ReviewInput",
                "global::Demo.ReviewInput",
                new []
                {
                    new InputFieldSerializerDescriptor(
                        "Commentary",
                        "commentary",
                        "SerializeNullableString"),
                    new InputFieldSerializerDescriptor(
                        "Stars",
                        "stars",
                        "SerializeNullableInt")
                },
                new[]
                {
                    new ValueSerializerDescriptor("String", "_stringSerializer"),
                    new ValueSerializerDescriptor("Int", "_intSerializer"),
                },
                new[]
                {
                    new InputTypeSerializerMethodDescriptor(
                        "SerializeNullableString",
                        true,
                        false,
                        "_stringSerializer",
                        null),
                    new InputTypeSerializerMethodDescriptor(
                        "SerializeNullableInt",
                        false,
                        false,
                        "_intSerializer",
                        null)
                });

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_With_List()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new InputModelSerializerGenerator();

            var descriptor = new InputModelSerializerDescriptor(
                "ReviewInputSerializer",
                "ReviewInput",
                "global::Demo.ReviewInput",
                new []
                {
                    new InputFieldSerializerDescriptor(
                        "Commentaries",
                        "commentaries",
                        "SerializeNullableStringList"),
                },
                new[]
                {
                    new ValueSerializerDescriptor("String", "_stringSerializer"),
                },
                new[]
                {
                    new InputTypeSerializerMethodDescriptor(
                        "SerializeNullableStringList",
                        true,
                        true,
                        null,
                        "SerializeNullableString"),
                    new InputTypeSerializerMethodDescriptor(
                        "SerializeNullableString",
                        true,
                        false,
                        "_stringSerializer",
                        null)
                });

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_With_NestedList()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new InputModelSerializerGenerator();

            var descriptor = new InputModelSerializerDescriptor(
                "ReviewInputSerializer",
                "ReviewInput",
                "global::Demo.ReviewInput",
                new []
                {
                    new InputFieldSerializerDescriptor(
                        "Commentaries",
                        "commentaries",
                        "SerializeNestedNullableStringList"),
                },
                new[]
                {
                    new ValueSerializerDescriptor("String", "_stringSerializer"),
                },
                new[]
                {
                    new InputTypeSerializerMethodDescriptor(
                        "SerializeNestedNullableStringList",
                        true,
                        true,
                        null,
                        "SerializeNullableStringList"),
                    new InputTypeSerializerMethodDescriptor(
                        "SerializeNullableStringList",
                        true,
                        true,
                        null,
                        "SerializeNullableString"),
                    new InputTypeSerializerMethodDescriptor(
                        "SerializeNullableString",
                        true,
                        false,
                        "_stringSerializer",
                        null)
                });

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public void CanHandle()
        {
            // arrange
            var generator = new InputModelSerializerGenerator();

            var descriptor = new InputModelSerializerDescriptor(
                "ReviewInputSerializer",
                "ReviewInput",
                "global::Demo.ReviewInput",
                new []
                {
                    new InputFieldSerializerDescriptor(
                        "Commentary",
                        "commentary",
                        "SerializeNullableString"),
                    new InputFieldSerializerDescriptor(
                        "Stars",
                        "stars",
                        "SerializeNullableInt")
                },
                new[]
                {
                    new ValueSerializerDescriptor("String", "_stringSerializer"),
                    new ValueSerializerDescriptor("Int", "_intSerializer"),
                },
                new[]
                {
                    new InputTypeSerializerMethodDescriptor(
                        "SerializeNullableString",
                        true,
                        false,
                        "_stringSerializer",
                        null),
                    new InputTypeSerializerMethodDescriptor(
                        "SerializeNullableInt",
                        false,
                        false,
                        "_intSerializer",
                        null)
                });

            // act
            var canHandle = generator.CanHandle(descriptor);

            // assert
            Assert.True(canHandle);
        }
    }
}
