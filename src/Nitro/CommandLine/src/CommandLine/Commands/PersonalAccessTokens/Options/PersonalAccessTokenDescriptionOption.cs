using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens.Options;

internal class PersonalAccessTokenDescriptionOption : Option<string>
{
    public PersonalAccessTokenDescriptionOption() : base("--description")
    {
        Description = "The description of the personal access token";
        Required = false;
        this.DefaultFromEnvironmentValue("DESCRIPTION");
    }
}

internal sealed class OptionalPersonalAccessTokenDescriptionOption : PersonalAccessTokenDescriptionOption
{
    public OptionalPersonalAccessTokenDescriptionOption() : base()
    {
        Required = false;
    }
}
