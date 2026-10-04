using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal sealed record AzureDevOpsActorDto(
    [property: JsonRequired] string Name,
    string? Email);
