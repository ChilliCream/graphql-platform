using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpClientBuilder
    {
        private string? _namespace;
        private string? _name;
        private ClientModel? _model;

        public CSharpClientBuilder SetNamespace(string @namespace)
        {
            _namespace = @namespace;
            return this;
        }

        public CSharpClientBuilder SetName(string name)
        {
            _name = name;
            return this;
        }

        public CSharpClientBuilder SetModel(ClientModel model)
        {
            _model = model;
            return this;
        }
    }
}
