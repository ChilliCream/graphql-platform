using System.CommandLine;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// A option for the schema command. The option is used to specify the name of the schema 
/// </summary>
internal sealed class SchemaNameOption : Option<string>
{
    public SchemaNameOption() : base("--schema-name")
    {
        Description = "The name of the schema that should be exported. If no schema name is " +
            "specified the default schema will be exported.";
    }
}
