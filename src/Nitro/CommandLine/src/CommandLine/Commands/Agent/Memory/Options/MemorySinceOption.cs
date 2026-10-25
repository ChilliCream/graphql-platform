using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;

internal sealed class MemorySinceOption : Option<DateTimeOffset?>
{
    public const string OptionName = "--since";

    public MemorySinceOption() : base(OptionName)
    {
        Description = "Only show memories updated at or after this RFC 3339 timestamp";
        Required = false;
        CustomParser = result =>
        {
            var value = result.Tokens.Single().Value;

            if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var parsed))
            {
                return parsed.ToUniversalTime();
            }

            result.AddError($"Option '{OptionName}' received an invalid value: {value}");
            return null;
        };
    }
}
