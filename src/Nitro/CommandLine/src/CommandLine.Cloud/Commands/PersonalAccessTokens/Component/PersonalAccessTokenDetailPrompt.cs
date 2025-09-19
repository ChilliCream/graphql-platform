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

    public PersonalAccessTokenDetailPromptResult ToObject()
    {
        return new PersonalAccessTokenDetailPromptResult
        {
            Id = _data.Id,
            Description = _data.Description,
            CreatedAt = _data.CreatedAt,
            ExpiresAt = _data.ExpiresAt
        };
    }

    public static PersonalAccessTokenDetailPrompt From(
        IPersonalAccessTokenDetailPrompt_PersonalAccessToken data)
        => new(data);

    public class PersonalAccessTokenDetailPromptResult
    {
        public required string Id { get; init; }

        public required string Description { get; init; }

        public required DateTimeOffset CreatedAt { get; init; }

        public required DateTimeOffset ExpiresAt { get; init; }
    }
}
