using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public interface ICSharpClientBuilderContext
    {
        string Namespace { get; }

        string Name { get; }

        ClientModel Model { get; }

        bool NullableRefTypes { get; }

        WellKnownTypes Types { get; }

        string GetFullTypeName(IInputType type, bool optional = false);

        string GetFullTypeName(ComplexOutputTypeModel type);

        string CreateFullTypeName(string name);

        string CreateTypeName(string name);
    }
}
