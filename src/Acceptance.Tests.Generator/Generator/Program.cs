using System;
using System.IO;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileContent = File.ReadAllText(
                Path.Combine("parsing", "SchemaParser.yaml"));
            
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
            ScenarioDefinition yamlObject = deserializer
                .Deserialize<ScenarioDefinition>(fileContent);
        }
    }
}
