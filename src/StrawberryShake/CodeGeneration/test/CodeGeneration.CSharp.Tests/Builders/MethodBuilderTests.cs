using System.Text;
using System.Threading.Tasks;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class MethodBuilderTests
    {
        [Fact]
        public async Task CreateMethod()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await MethodBuilder.New()
                .SetName("GetAbc")
                .AddCode(CodeLineBuilder.New().SetLine("return;"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateMethod_With_One_Parameter()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await MethodBuilder.New()
                .SetName("GetAbc")
                .AddCode(CodeLineBuilder.New().SetLine("return;"))
                .AddParameter(ParameterBuilder.New().SetName("abc").SetType("String"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateMethod_With_Two_Parameter()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await MethodBuilder.New()
                .SetName("GetAbc")
                .AddCode(CodeLineBuilder.New().SetLine("return;"))
                .AddParameter(ParameterBuilder.New().SetName("abc").SetType("String"))
                .AddParameter(ParameterBuilder.New().SetName("def").SetType("String").SetDefault())
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateMethod_With_ReturnType()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await MethodBuilder.New()
                .SetName("GetAbc")
                .SetReturnType("Int32")
                .AddCode(CodeLineBuilder.New().SetLine("return;"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [InlineData(AccessModifier.Public)]
        [InlineData(AccessModifier.Internal)]
        [InlineData(AccessModifier.Protected)]
        [InlineData(AccessModifier.Private)]
        [Theory]
        public async Task CreateMethod_With_AccessModifier(AccessModifier accessModifier)
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await MethodBuilder.New()
                .SetName("GetAbc")
                .SetAccessModifier(accessModifier)
                .AddCode(CodeLineBuilder.New().SetLine("return;"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot(
                new SnapshotNameExtension(
                    accessModifier.ToString()));
        }

        [InlineData(Inheritance.None)]
        [InlineData(Inheritance.Override)]
        [InlineData(Inheritance.Sealed)]
        [InlineData(Inheritance.Virtual)]
        [Theory]
        public async Task CreateMethod_With_Inheritance(Inheritance inheritance)
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await MethodBuilder.New()
                .SetName("GetAbc")
                .SetInheritance(inheritance)
                .AddCode(CodeLineBuilder.New().SetLine("return;"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot(
                new SnapshotNameExtension(
                    inheritance.ToString()));
        }

        [Fact]
        public async Task CreateExtensionMethod()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await MethodBuilder.New()
                .SetName("GetAbc")
                .SetStatic()
                .AddParameter(ParameterBuilder.New().SetName("abc").SetType("this String"))
                .AddCode(CodeLineBuilder.New().SetLine("return;"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }
    }
}
