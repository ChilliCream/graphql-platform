using Zeus.Types;

namespace Zeus
{
    public interface ISchema
    {
        ObjectDeclaration Query { get; }
        ObjectDeclaration Mutation { get; }

        bool TryGetObjectType(string typeName, out ObjectDeclaration objectType);
        bool TryGetInputType(string typeName, out InputDeclaration inputType);
        bool TryGetResolver(string typeName, string fieldName, out IResolver resolver);
    }
}

