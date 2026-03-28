namespace ChilliCream.Nitro.CommandLine.Options;

internal class MockSchemaNameOption : Option<string>
{
    public MockSchemaNameOption() : base("--name")
    {
        Description = "The name of the mock schema";
        Required = true;
        this.DefaultFromEnvironmentValue("MOCK_SCHEMA_NAME");
        this.NonEmptyStringsOnly();
    }
}
