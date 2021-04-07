using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace StrawberryShake.Tools.Configuration
{
    public class GraphQLConfig
    {
        public string Schema { get; set; } = "schema.graphql";

        public string Documents { get; set; } = "**/*.graphql";

        public GraphQLConfigExtensions Extensions { get; set; } =
            new GraphQLConfigExtensions();

        public void Save(string fileName)
        {
            if(!Directory.Exists(fileName))
            {
                Directory.CreateDirectory(fileName);
            }

            File.WriteAllText(
                fileName,
                JsonSerializer.Serialize(
                    this,
                    new()
                    {
                        WriteIndented = true,
                        IgnoreNullValues = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }));
        }
    }

    public class GraphQLConfigExtensions
    {
        public StrawberryShakeSettings StrawberryShake { get; set; } =
            new StrawberryShakeSettings();
    }

    public class StrawberryShakeSettings
    {
        public string Name { get; set; }

        public string Namespace { get; set; }

        public string Url { get; set; }

        public bool NoStore { get; set; } = false;

        public bool EmitGeneratedCode { get; set; } = true;

        public StrawberryShakeSettingsRecords Records { get; } =
            new StrawberryShakeSettingsRecords
            {
                Inputs = false,
                Entities = false
            };

        public List<StrawberryShakeTransportSettings> TransportProfiles { get; } =
            new List<StrawberryShakeTransportSettings>
            {
                new StrawberryShakeTransportSettings
                {
                    Default = TransportType.Http,
                    Subscription = TransportType.WebSocket
                }
            };
    }

    /// <summary>
    /// This settings class defines which parts shall be generated as records.
    /// </summary>
    public class StrawberryShakeSettingsRecords
    {
        /// <summary>
        /// Defines if the generator shall generate records for input types.
        /// </summary>
        public bool Inputs { get; set; }

        /// <summary>
        /// Defines if the generator shall generate records for entities.
        /// </summary>
        public bool Entities { get; set; }
    }


    public class StrawberryShakeTransportSettings
    {
        public string Name { get; set; } = default!;

        public TransportType Default { get; set; } = TransportType.Http;

        public TransportType? Query { get; set; }

        public TransportType? Mutation { get; set; }

        public TransportType? Subscription { get; set; }
    }

    public enum TransportType
    {
        Http = 0,
        WebSocket = 1,
        InMemory = 2,
        SignalR = 3,
        Grpc = 4
    }
}
