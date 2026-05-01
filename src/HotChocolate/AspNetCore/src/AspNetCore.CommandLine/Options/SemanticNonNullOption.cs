using System.CommandLine;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// An option for the schema command. The option is used to rewrite non-null
/// output fields to use the @semanticNonNull directive in the exported schema.
/// </summary>
internal sealed class SemanticNonNullOption : Option<bool>
{
    public SemanticNonNullOption() : base("--semantic-non-null")
    {
        Description = "Rewrite the exported schema to strip non-null wrappers from output "
            + "fields and apply the @semanticNonNull directive instead.";
    }
}
