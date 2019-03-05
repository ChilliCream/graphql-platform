using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Generator
{
    internal class Deserializer
    {
        internal static IDeserializer Instance { get; } =
            new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .WithNamingConvention(new HyphenatedNamingConvention())
                .Build();
    }
}
