using System.Text;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Serialization;

public class SchemaDebugFormatterTests
{
    [Fact]
    public void Format_Should_PrependDeprecatedDirective_When_ObjectTypeIsDeprecated()
    {
        // arrange
        // the deprecation state is set programmatically, so the formatter synthesizes @deprecated
        const string sdl =
            """
            type Foo implements Bar @example {
              id: ID
            }

            interface Bar {
              id: ID
            }
            """;
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var fooType = (MutableObjectTypeDefinition)schema.Types["Foo"];
        fooType.DeprecationReason = "Use Bar.";

        // act
        var syntaxNode = SchemaDebugFormatter.Format(fooType);

        // assert
        syntaxNode.ToString().MatchInlineSnapshot(
            """
            type Foo implements Bar @deprecated(reason: "Use Bar.") @example {
              id: ID
            }
            """);
    }

    [Fact]
    public void Format_Should_UseDefaultReason_When_ObjectTypeHasNoDeprecationReason()
    {
        // arrange
        // the deprecation state is set programmatically, so the formatter synthesizes @deprecated
        const string sdl =
            """
            type Foo {
              id: ID
            }
            """;
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var fooType = (MutableObjectTypeDefinition)schema.Types["Foo"];
        fooType.IsDeprecated = true;

        // act
        var syntaxNode = SchemaDebugFormatter.Format(fooType);

        // assert
        syntaxNode.ToString().MatchInlineSnapshot(
            """
            type Foo @deprecated(reason: "No longer supported.") {
              id: ID
            }
            """);
    }
}
