namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// The repeatable <c>--coordinate</c> option used by coordinate-scoped analytical commands.
/// Accepts schema coordinates such as <c>User.email</c> or <c>Query.users(filter:)</c>. Each
/// occurrence appends a coordinate to the request.
/// </summary>
internal sealed class CoordinateOption : Option<List<string>>
{
    public const string OptionName = "--coordinate";

    public CoordinateOption() : base(OptionName)
    {
        Description =
            "A schema coordinate such as 'User.email' or 'Query.users'. "
            + "Pass --coordinate multiple times to query several coordinates in one call.";
        Required = true;
        AllowMultipleArgumentsPerToken = true;
    }
}
