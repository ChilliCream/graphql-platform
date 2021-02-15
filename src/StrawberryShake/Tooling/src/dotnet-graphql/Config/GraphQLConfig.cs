namespace StrawberryShake.Tools.Config
{
    public class GraphQLConfig
    {
        public string Schema { get; set; } = default!;

        public string? Documents { get; set; }

        public string Location { get; set; } = default!;

        public GraphQLConfigExtensions Extensions { get; set; } = default!;
    }
}
