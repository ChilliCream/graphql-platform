using System.CommandLine;
using HotChocolate.Serialization;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// An option for selecting the GraphQL specification edition of the exported schema.
/// </summary>
internal sealed class SpecVersionOption : Option<string>
{
    public SpecVersionOption() : base("--spec-version")
    {
        Description = "Downgrade the exported schema to the SDL grammar of a GraphQL specification "
            + "edition. Supported values: october-2021, september-2025.";

        Validators.Add(result =>
        {
            var value = result.GetValue(this);

            if (value is not null && !GraphQLSpecVersions.TryParse(value, out _))
            {
                result.AddError(
                    $"Unknown spec version '{value}'. Supported values: october-2021, september-2025.");
            }
        });
    }
}
