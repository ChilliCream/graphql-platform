namespace HotChocolate.Types.Mutable;

public class DeprecatedMutableDirectiveDefinitionTests
{
    [Fact]
    public void Reason_Should_BeNonNullWithTheDefaultReason()
    {
        // arrange
        var schema = new MutableSchemaDefinition();

        // act
        var deprecated = BuiltIns.Deprecated.Create(schema);

        // assert
        Assert.Equal("String!", deprecated.Reason.Type.Print());
        Assert.Equal("\"No longer supported\"", deprecated.Reason.DefaultValue?.ToString());
    }
}
