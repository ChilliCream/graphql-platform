namespace ChilliCream.Nitro.CommandLine.Commands.Config;

/// <summary>
/// The positional key argument accepted by <c>nitro config unset</c>. Identifies which
/// default should be cleared.
/// </summary>
internal sealed class UnsetConfigKeyArgument : Argument<string>
{
    public const string ArgumentName = "KEY";
    public const string Api = "api";
    public const string Stage = "stage";
    public const string Format = "format";

    public static readonly string[] All = [Api, Stage, Format];

    public UnsetConfigKeyArgument() : base(ArgumentName)
    {
        Description = "The default to clear (api, stage, format).";

        this.AcceptOnlyFromAmong(All);
    }
}
