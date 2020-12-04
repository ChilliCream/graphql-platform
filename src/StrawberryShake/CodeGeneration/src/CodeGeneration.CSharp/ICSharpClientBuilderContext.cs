using HotChocolate.Language;
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

        string GetFullTypeName(IOutputType type, SelectionSetNode? selectionSet);

        string GetFullTypeName(string typeName);

        string GetSerializationTypeName(IType type);

        string CreateTypeName(string typeName);

        bool IsReferenceType(IOutputType type, SelectionSetNode? selectionSet);
    }
}
