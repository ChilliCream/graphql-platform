namespace ChilliCream.Nitro.CommandLine.Options;

internal class MockSchemaIdOption : Option<string>
{
    public MockSchemaIdOption() : base("--mock-id")
    {
        Description = "The ID of the API";
        Required = true;
        this.DefaultFromEnvironmentValue("MOCK_ID");
        this.NonEmptyStringsOnly();
    }
}
