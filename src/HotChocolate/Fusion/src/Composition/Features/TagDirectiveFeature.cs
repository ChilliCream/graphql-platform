using System.Text.Json;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition.Features;

public sealed class TagDirectiveFeature : IFusionFeature, IFusionFeatureParser<TagDirectiveFeature>
{
    public TagDirectiveFeature(
        IEnumerable<string>? tags = null,
        TagMode mode = TagMode.Exclude,
        bool makeTagsPublic = false)
    {
        Tags = new HashSet<string>(tags ?? Enumerable.Empty<string>());
        Mode = mode;
        MakeTagsPublic = makeTagsPublic;
    }

    public IReadOnlySet<string> Tags { get; }

    public TagMode Mode { get; }

    /// <summary>
    /// Defines if the tag directives should be exported to the public schema.
    /// </summary>
    public bool MakeTagsPublic { get; }

    public static TagDirectiveFeature Parse(JsonElement value)
    {
        if (value.TryGetProperty("type", out var type) && type.GetString() == "tagDirective")
        {
            var tags = new List<string>();
            bool makeTagsPublic = false;

            if (value.TryGetProperty("tags", out var tagsElement))
            {
                foreach (var tag in tagsElement.EnumerateArray())
                {
                    if (tag.ValueKind == JsonValueKind.String &&
                        tag.GetString() is { Length: > 0 } s &&
                        s.IsValidGraphQLName())
                    {
                        tags.Add(s);
                    }
                }
            }

            if (!value.TryGetProperty("mode", out var modeElement) ||
                !Enum.TryParse<TagMode>(modeElement.GetString(), out var mode))
            {
                mode = TagMode.Exclude;
            }

            if (!value.TryGetProperty("makeTagsPublic", out var publicElement) &&
                publicElement.GetBoolean())
            {
                makeTagsPublic = true;
            }


            return new TagDirectiveFeature(tags, mode, makeTagsPublic);
        }

        throw new InvalidOperationException("The value is not a tagDirective feature configuration section.");
    }
}