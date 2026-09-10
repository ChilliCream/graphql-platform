using System.Net.Http.Headers;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalBatchingFormatListOption : Option<List<string>>
{
    public const string OptionName = "--batching-format";

    public OptionalBatchingFormatListOption() : base(OptionName)
    {
        Description = "One or more response formats the source schema supports for batching";
        Arity = ArgumentArity.OneOrMore;
        AllowMultipleArgumentsPerToken = true;
        Validators.Add(result =>
        {
            foreach (var token in result.Tokens)
            {
                if (!IsValid(token.Value))
                {
                    result.AddError(FormatError(token.Value));
                }
            }
        });
    }

    private static bool IsValid(string value)
        => MediaTypeHeaderValue.TryParse(value, out _);

    private static string FormatError(string value)
        => $"The value '{value}' for '{OptionName}' must be a valid media type.";
}
