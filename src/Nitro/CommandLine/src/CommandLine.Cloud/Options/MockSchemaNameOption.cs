namespace ChilliCream.Nitro.CLI.Option;

internal class MockSchemaNameOption : Option<string>
{
    public MockSchemaNameOption() : base("--name")
    {
        Description = "The name of the mock schema";
        IsRequired = true;
        this.DefaultFromEnvironmentValue("MOCK_SCHEMA_NAME");
    }
}
