using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using StrawberryShake.Tools.Configuration;
using IOPath = System.IO.Path;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.Tests
{

    public class Tests
    {
        [Fact]
        public void Deserialization()
        {
            string fileName = "/Users/michael/local/hc/src/StrawberryShake/SourceGenerator/test/CodeGeneration.CSharp.Analyzers.Tests/obj/berry/StarWarsClient.code";

            if (File.Exists(fileName))
            {
                try
                {
                    var x = JsonConvert.DeserializeObject<List<SourceDocument>>(
                        File.ReadAllText(fileName));
                }
                catch
                {
                    // we ignore any error here.
                }
            }
        }
    }
}