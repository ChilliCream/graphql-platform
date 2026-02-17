namespace ChilliCream.Nitro.CommandLine.Options;

internal class MockSchemaIdOption : Option<string>
{
    public MockSchemaIdOption() : base("--mock-id", "The id of the api")
    {
        IsRequired = true;
        this.DefaultFromEnvironmentValue("MOCK_ID");
    }
}
