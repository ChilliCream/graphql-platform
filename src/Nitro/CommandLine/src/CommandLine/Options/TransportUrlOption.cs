namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// An option that holds a source schema transport URL. A value containing a <c>{{VARIABLE}}</c>
/// reference is not validated as a URL.
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
