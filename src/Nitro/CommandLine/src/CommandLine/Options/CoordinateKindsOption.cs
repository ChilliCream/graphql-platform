namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// The repeatable <c>--kinds</c> option used by <c>nitro schema unused</c>. Filters the
/// search to specific GraphQL coordinate kinds. The accepted values match the
/// <c>CoordinateKind</c> enum in the Nitro schema.
/// </summary>
internal sealed class CoordinateKindsOption : Option<List<string>>
{
    public const string OptionName = "--kinds";

    public CoordinateKindsOption() : base(OptionName)
    {
        Description =
            "One or more coordinate kinds to include "
            + "(OBJECT_FIELD, INTERFACE_FIELD, ENUM_VALUE, ...). "
            + "Defaults to OBJECT_FIELD and INTERFACE_FIELD.";
        Required = false;
        AllowMultipleArgumentsPerToken = true;
    }
}
