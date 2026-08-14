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

            if (value is not null && !IsValid(value))
            {
                result.AddError(Messages.TransportUrlInvalid(name));
            }
        });
    }

    /// <summary>
    /// Determines whether the value is an absolute HTTP URL without user information or a
    /// fragment. A value containing a <c>{{VARIABLE}}</c> reference is always accepted.
    /// </summary>
    public static bool IsValid(string value)
    {
        if (value.Contains("{{", StringComparison.Ordinal))
        {
            return true;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var url)
            && url.Scheme is "http" or "https"
            && string.IsNullOrEmpty(url.UserInfo)
            && string.IsNullOrEmpty(url.Fragment);
    }
}
