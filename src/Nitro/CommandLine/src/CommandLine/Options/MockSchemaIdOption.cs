namespace ChilliCream.Nitro.CommandLine.Options;

internal class MockSchemaIdOption : Option<string>
{
    public MockSchemaIdOption() : base("--mock-id", "The ID of the API")
    {
        IsRequired = true;
        this.DefaultFromEnvironmentValue("MOCK_ID");
        this.NonEmptyStringsOnly();
    }
}
