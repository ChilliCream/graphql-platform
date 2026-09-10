namespace HotChocolate;

public class SchemaOptionsTests
{
    [Fact]
    public void FromOptions_Should_CopyEveryOption_When_OptionsAreConfigured()
    {
        // arrange
        var options = new SchemaOptions
        {
            QueryTypeName = "Q",
            StrictValidation = false,
            EnableObjectDeprecation = true,
            EnableFlagEnums = true,
            SortFieldsByName = true
        };

        // act
        var copy = SchemaOptions.FromOptions(options);

        // assert
        copy.MatchSnapshot();
    }
}
