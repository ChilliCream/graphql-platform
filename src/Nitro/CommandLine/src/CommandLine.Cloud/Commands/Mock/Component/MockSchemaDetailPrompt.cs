using ChilliCream.Nitro.CommandLine.Cloud.Client;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Mock.Component;

internal sealed class MockSchemaDetailPrompt(IMockSchemaDetailPrompt data)
{
    public MockSchemaDetailPromptResult ToObject()
    {
        return new MockSchemaDetailPromptResult
        {
            Id = data.Id,
            Name = data.Name,
            Url = data.Url,
            DownstreamUrl = data.DownstreamUrl.ToString(),
            CreatedBy = new CreatedBy { Username = data.CreatedBy.Username, CreatedAt = data.CreatedAt },
            ModifiedBy = new ModifiedBy { Username = data.ModifiedBy.Username, ModifiedAt = data.ModifiedAt }
        };
    }

    public static MockSchemaDetailPrompt From(IMockSchemaDetailPrompt data)
        => new(data);

    public class MockSchemaDetailPromptResult
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public required string Url { get; init; }

        public required string DownstreamUrl { get; init; }

        public required CreatedBy CreatedBy { get; init; }

        public required ModifiedBy ModifiedBy { get; init; }
    }

    public class CreatedBy
    {
        public required string Username { get; init; }

        public required DateTimeOffset CreatedAt { get; init; }
    }

    public class ModifiedBy
    {
        public required string Username { get; init; }

        public required DateTimeOffset ModifiedAt { get; init; }
    }
}
