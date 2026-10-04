namespace ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens.Options;

internal sealed class OptionalPersonalAccessTokenDescriptionOption : PersonalAccessTokenDescriptionOption
{
    public OptionalPersonalAccessTokenDescriptionOption() : base()
    {
        Required = false;
    }
}
