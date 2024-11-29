#nullable enable
namespace HotChocolate.Types;

internal sealed class TagOptions
{
    public TagMode Mode { get; set; } = TagMode.GraphQLFusion;
}
