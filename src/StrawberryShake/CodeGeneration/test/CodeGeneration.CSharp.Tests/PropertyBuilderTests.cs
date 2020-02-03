using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
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

    public class ClassBuilderTests
    {
        [Fact]
        public async Task Class_With_One_Property()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await ClassBuilder.New()
                .SetName("MyClass")
                .AddProperty(
                    PropertyBuilder.New()
                    .SetName("Foo")
                    .SetType("Bar"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Class_With_One_Property_Implements()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await ClassBuilder.New()
                .SetName("MyClass")
                .AddImplements("SomeOtherType")
                .AddProperty(
                    PropertyBuilder.New()
                    .SetName("Foo")
                    .SetType("Bar"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Class_With_One_Property_Implements_Sealed()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await ClassBuilder.New()
                .SetName("MyClass")
                .AddImplements("SomeOtherType")
                .SetSealed()
                .AddProperty(
                    PropertyBuilder.New()
                    .SetName("Foo")
                    .SetType("Bar"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Class_With_One_Property_Implements_Abstract()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await ClassBuilder.New()
                .SetName("MyClass")
                .AddImplements("SomeOtherType")
                .SetAbstract()
                .AddProperty(
                    PropertyBuilder.New()
                    .SetName("Foo")
                    .SetType("Bar"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Class_With_Field_Constructor_Property_Method()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await ClassBuilder.New()
                .SetName("MyClass")
                .AddImplements("SomeOtherType")
                .SetAbstract()
                .AddField(FieldBuilder.New()
                    .SetName("_foo")
                    .SetType("Bar")
                    .SetReadOnly())
                .AddProperty(
                    PropertyBuilder.New()
                    .SetName("Foo")
                    .SetType("Bar")
                    .SetBackingField("_foo"))
                .AddConstructor(ConstructorBuilder.New()
                    .SetAccessModifier(AccessModifier.Protected)
                    .AddParameter(ParameterBuilder.New()
                        .SetName("foo")
                        .SetType("Bar")
                        .SetDefault())
                    .AddCode("_foo = foo;"))
                .AddMethod(MethodBuilder.New()
                    .SetName("GetFooAsync")
                    .SetReturnType("ValueTask<Bar>")
                    .AddCode("return new ValueTask<Bar>(_foo);"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }
    }
}
