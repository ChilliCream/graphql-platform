using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Generator
{
    internal class Deserializer
    {
        private static readonly IDeserializer _deserializer =
            new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .WithNamingConvention(new HyphenatedNamingConvention())
                .Build();

        public static Scenario Deserialize(string value)
        {
            Dictionary<string, object> scenario = _deserializer
                .Deserialize<Dictionary<string, object>>(value);

            if (!scenario.ContainsKey("scenario"))
            {
                throw new InvalidOperationException("Scenario must have a title");
            }

            if (!scenario.ContainsKey("tests"))
            {
                throw new InvalidOperationException("Scenario must have tests");
            }

            var name = scenario["scenario"] as string;
            IEnumerable<Test> tests = TestResolver.Resolve(scenario["tests"]);

            return new Scenario(name, tests);
        }
    }
}
