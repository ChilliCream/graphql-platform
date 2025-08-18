using ChilliCream.Nitro.CommandLine.Cloud.Client;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Mock.Component;

internal sealed class MockSchemaDetailPrompt(IMockSchemaDetailPrompt data)
{
    public object ToObject()
    {
        return new
        {
            data.Id,
            data.Name,
            data.Url,
            data.DownstreamUrl,
            CreatedBy = new { data.CreatedBy.Username, data.CreatedAt },
            ModifiedBy = new { data.ModifiedBy.Username, data.ModifiedAt }
        };
    }

    public static MockSchemaDetailPrompt From(IMockSchemaDetailPrompt data)
        => new(data);
}
