using System.Text.Json.Serialization;
using HotChocolate.Fusion.Composition.Features;

namespace HotChocolate.Fusion.Composition.Settings;

internal class PackageSettings
{
    private Feature? _reEncodeIds;
    private Feature? _nodeField;
    private TagDirective? _tagDirective;
    private Transport? _transport;

    [JsonPropertyName("fusionTypePrefix")]
    [JsonPropertyOrder(10)]
    public string? FusionTypePrefix { get; set; }

    [JsonPropertyName("fusionTypeSelf")]
    [JsonPropertyOrder(11)]
    public bool FusionTypeSelf { get; set; }

    public Transport Transport
    {
        get => _transport ??= new();
        set => _transport = value;
    }

    [JsonPropertyName("nodeField")]
    [JsonPropertyOrder(12)]
    public Feature NodeField
    {
        get => _nodeField ??= new();
        set => _nodeField = value;
    }

    [JsonPropertyName("reEncodeIds")]
    [JsonPropertyOrder(13)]
    public Feature ReEncodeIds
    {
        get => _reEncodeIds ??= new();
        set => _reEncodeIds = value;
    }

    [JsonPropertyName("tagDirective")]
    [JsonPropertyOrder(14)]
    public TagDirective TagDirective
    {
        get => _tagDirective ??= new();
        set => _tagDirective = value;
    }

    public FusionFeatureCollection CreateFeatures()
    {
        var features = new List<IFusionFeature>
        {
            new TransportFeature
            {
                DefaultClientName = Transport.DefaultClientName,
            },
        };

        if (NodeField.Enabled)
        {
            features.Add(FusionFeatures.NodeField);
        }

        if (ReEncodeIds.Enabled)
        {
            features.Add(FusionFeatures.ReEncodeIds);
        }

        if (TagDirective.Enabled)
        {
            features.Add(
                FusionFeatures.TagDirective(
                    TagDirective.Exclude,
                    TagDirective.MakePublic));
        }

        return new FusionFeatureCollection(features);
    }
}
