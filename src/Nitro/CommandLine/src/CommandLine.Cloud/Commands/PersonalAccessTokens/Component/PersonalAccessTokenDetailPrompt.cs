using ChilliCream.Nitro.CommandLine.Cloud.Client;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class PersonalAccessTokenDetailPrompt
{
    private readonly IPersonalAccessTokenDetailPrompt_PersonalAccessToken _data;

    private PersonalAccessTokenDetailPrompt(
        IPersonalAccessTokenDetailPrompt_PersonalAccessToken data)
    {
        _data = data;
    }

    public object ToObject()
    {
        return new { _data.Id, _data.Description, _data.CreatedAt, _data.ExpiresAt };
    }

    public static PersonalAccessTokenDetailPrompt From(
        IPersonalAccessTokenDetailPrompt_PersonalAccessToken data)
        => new(data);
}
