using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class PropertyBuilderTests
    {
        [Fact]
        public async Task CreateReadOnlyAutoProperty()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await PropertyBuilder.New()
                .SetName("Foo")
                .SetType("Bar")
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateAutoProperty()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await PropertyBuilder.New()
                .SetName("Foo")
                .SetType("Bar")
                .MakeSettable()
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateAutoProperty_With_Private_Setter()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await PropertyBuilder.New()
                .SetName("Foo")
                .SetType("Bar")
                .MakeSettable()
                .SetSetterAccessModifier(AccessModifier.Private)
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateProperty_With_Backing_Field()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await PropertyBuilder.New()
                .SetName("Foo")
                .SetType("Bar")
                .SetBackingField("_foo")
                .MakeSettable()
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateProperty_With_Backing_Field_And_Private_Setter()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await PropertyBuilder.New()
                .SetName("Foo")
                .SetType("Bar")
                .SetBackingField("_foo")
                .SetSetterAccessModifier(AccessModifier.Private)
                .MakeSettable()
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateProperty_With_Backing_Field_And_Custom_Setter()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await PropertyBuilder.New()
                .SetName("Foo")
                .SetType("Bar")
                .SetBackingField("_foo")
                .SetSetterAccessModifier(AccessModifier.Private)
                .SetSetter(CodeLineBuilder.New().SetLine("_value = value;"))
                .MakeSettable()
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateProperty_With_Custom_Getter_And_Custom_Setter()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await PropertyBuilder.New()
                .SetName("Foo")
                .SetType("Bar")
                .SetGetter(CodeLineBuilder.New().SetLine("return _value;"))
                .SetSetterAccessModifier(AccessModifier.Private)
                .SetSetter(CodeLineBuilder.New().SetLine("_value = value;"))
                .MakeSettable()
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }
    }
}
