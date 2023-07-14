using System.Collections;
using System.Text.Json;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition;

[Flags]
public enum FusionFeatureFlags
{
    None = 0,
    NodeField = 1,
    ReEncodeAllIds = 2,
    ApplyTagDirective = 4,
}

public interface IFusionFeature { }

public interface IFusionFeatureParser<out T>
{
    static abstract T Parse(JsonElement value);
}

public sealed class NodeFieldFeature : IFusionFeature, IFusionFeatureParser<NodeFieldFeature>
{
    private NodeFieldFeature() { }

    public static NodeFieldFeature Instance { get; } = new();

    public static NodeFieldFeature Parse(JsonElement value)
    {
        if (value.TryGetProperty("type", out var type) && type.GetString() == "nodeFieldSupport")
        {
            return Instance;
        }

        throw new InvalidOperationException("The value is not a node field feature configuration section.");
    }
}

public sealed class ReEncodeIdsFeature : IFusionFeature, IFusionFeatureParser<ReEncodeIdsFeature>
{
    private ReEncodeIdsFeature() { }

    public static ReEncodeIdsFeature Instance { get; } = new();

    public static ReEncodeIdsFeature Parse(JsonElement value)
    {
        if (value.TryGetProperty("type", out var type) && type.GetString() == "reEncodeIds")
        {
            return Instance;
        }

        throw new InvalidOperationException("The value is not a reEncodeIds feature configuration section.");
    }
}

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

public enum TagMode
{
    Exclude = 0,
    Include = 1,
}

public sealed class FusionFeatureCollection : IReadOnlyList<IFusionFeature>
{
    private readonly Dictionary<Type, IFusionFeature> _features = new();

    public FusionFeatureCollection(IEnumerable<IFusionFeature> features)
    {
        if (features == null)
        {
            throw new ArgumentNullException(nameof(features));
        }

        foreach (var feature in features)
        {
            _features[feature.GetType()] = feature;
        }
    }

    public IFusionFeature this[int index] => throw new NotImplementedException();

    public bool IsSupported<TFeature>(IFusionFeature feature) where TFeature : IFusionFeature
        => _features.TryGetValue(typeof(TFeature), out var registeredFeature) &&
            registeredFeature.Equals(feature);

    public bool IsSupported<TFeature>() where TFeature : IFusionFeature
        => _features.ContainsKey(typeof(TFeature));

    public bool TryGetFeature<TFeature>(out TFeature feature) where TFeature : IFusionFeature
    {
        if (_features.TryGetValue(typeof(TFeature), out var registeredFeature))
        {
            feature = (TFeature) registeredFeature;
            return true;
        }

        feature = default!;
        return false;
    }

    public int Count => _features.Count;

    public IEnumerator<IFusionFeature> GetEnumerator()
        => _features.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}

public static class FusionFeatureCollectionExtensions
{
    public static bool IsNodeFieldSupported(this FusionFeatureCollection features)
        => features.IsSupported<NodeFieldFeature>();
    
    public static bool MakeTagsPublic(this FusionFeatureCollection features)
        => features.TryGetFeature<TagDirectiveFeature>(out var feature) &&
            feature.MakeTagsPublic;
    
    public static FusionFeatureCollection Parse(this JsonElement value)
    {
        var features = new List<IFusionFeature>();

        foreach (var feature in value.EnumerateArray())
        {
            if (feature.TryGetProperty("type", out var type))
            {
                if (type.GetString() == "nodeFieldSupport")
                {
                    features.Add(NodeFieldFeature.Parse(feature));
                }
                else if (type.GetString() == "reEncodeIds")
                {
                    features.Add(ReEncodeIdsFeature.Parse(feature));
                }
                else if (type.GetString() == "tagDirective")
                {
                    features.Add(TagDirectiveFeature.Parse(feature));
                }
            }
        }

        return new FusionFeatureCollection(features);
    }
}