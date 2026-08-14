namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// An option that holds a source schema transport URL. A value that references an environment
/// variable is left to composition to resolve and is therefore not validated as a URL.
/// </summary>
internal abstract class TransportUrlOption : Option<string>
{
    protected TransportUrlOption(string name) : base(name)
    {
        Validators.Add(result =>
        {
            var value = result.GetValue(this);

            if (value is null || value.Contains("{{", StringComparison.Ordinal))
            {
                return;
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var url)
                || url.Scheme is not ("http" or "https")
                || !string.IsNullOrEmpty(url.UserInfo)
                || !string.IsNullOrEmpty(url.Fragment))
            {
                result.AddError(Messages.TransportUrlInvalid(name));
            }
        });
    }
}

internal sealed class OptionalTransportUrlOption : TransportUrlOption
{
    public const string OptionName = "--url";

    public OptionalTransportUrlOption() : base(OptionName)
    {
        Description = "The URL the gateway uses to reach the source schema";
    }
}

internal sealed class OptionalTransportDevUrlOption : TransportUrlOption
{
    public const string OptionName = "--dev-url";

    public OptionalTransportDevUrlOption() : base(OptionName)
    {
        Description = "The URL a local development environment uses to reach the source schema";
    }
}
