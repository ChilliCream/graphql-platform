using System.Text;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class SourceResponseNameMappingTests
{
    [Fact]
    public void ResponseNameUtf8_Should_ExposeReadOnlySpan()
    {
        // arrange
        var mapping = new SourceResponseNameMapping(
            fieldName: "field",
            sourceResponseName: "sourceField",
            responseName: "clientField");
        var property = typeof(SourceResponseNameMapping).GetProperty(
            nameof(SourceResponseNameMapping.ResponseNameUtf8));

        // act
        var snapshot =
            $$"""
            Property type: {{property!.PropertyType}}
            Can write: {{property.CanWrite}}
            UTF-8 value: {{Encoding.UTF8.GetString(mapping.ResponseNameUtf8)}}
            """;

        // assert
        snapshot.MatchInlineSnapshot(
            """
            Property type: System.ReadOnlySpan`1[System.Byte]
            Can write: False
            UTF-8 value: clientField
            """);
    }
}
