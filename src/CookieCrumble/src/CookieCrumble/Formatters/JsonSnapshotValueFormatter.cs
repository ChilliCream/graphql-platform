using System.Buffers;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace CookieCrumble.Formatters;

internal sealed class JsonSnapshotValueFormatter : ISnapshotValueFormatter, IMarkdownSnapshotValueFormatter
{
    private static readonly JsonSerializerSettings _settings =
        new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Culture = CultureInfo.InvariantCulture,
            ContractResolver = ChildFirstContractResolver.Instance,
            Converters = new List<JsonConverter> { new StringEnumConverter(), },
        };

    public bool CanHandle(object? value)
        => true;

    public void Format(IBufferWriter<byte> snapshot, object? value)
        => snapshot.Append(JsonConvert.SerializeObject(value, _settings));

    public void FormatMarkdown(IBufferWriter<byte> snapshot, object? value)
    {
        snapshot.Append("```json");
        snapshot.AppendLine();
        Format(snapshot, value);
        snapshot.AppendLine();
        snapshot.Append("```");
        snapshot.AppendLine();
    }

    private class ChildFirstContractResolver : DefaultContractResolver
    {
        static ChildFirstContractResolver() { Instance = new ChildFirstContractResolver(); }

        public static ChildFirstContractResolver Instance { get; }

        protected override IList<JsonProperty> CreateProperties(
            Type type,
            MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);

            properties = properties.OrderBy(p =>
            {
                var d = p.DeclaringType!.BaseTypesAndSelf().ToList();
                return 1000 - d.Count;
            }).ToList();

            return properties!;
        }
    }
}
