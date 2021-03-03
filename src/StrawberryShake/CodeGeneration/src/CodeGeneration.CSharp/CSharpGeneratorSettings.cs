using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorSettings
    {
        public string ClientName { get; set; } = "GraphQLClient";

        public string Namespace { get; set; } = "StrawberryShake.GraphQL";

        public bool StrictSchemaValidation { get; } = true;

        public IDocumentHashProvider HashProvider { get; set; } = new Sha1DocumentHashProvider();
    }
}
