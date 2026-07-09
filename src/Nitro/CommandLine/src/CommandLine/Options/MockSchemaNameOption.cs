namespace ChilliCream.Nitro.CommandLine;

internal class MockSchemaNameOption : Option<string>
{
    public MockSchemaNameOption() : base("--name")
    {
        Description = "The name of the mock schema";
        Required = true;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.MockSchemaName);
        this.NonEmptyStringsOnly();
    }
}
