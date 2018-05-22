using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate
{
    // TODO : move under configuration?
    public interface ISchemaContext
    {
        INamedType GetType(string typeName);
        T GetType<T>(string typeName)
            where T : INamedType;

        IReadOnlyCollection<INamedType> GetAllTypes();

        IOutputType GetOutputType(string typeName);

        T GetOutputType<T>(string typeName)
            where T : IOutputType;

        bool TryGetOutputType<T>(string typeName, out T type)
            where T : IOutputType;

        IInputType GetInputType(string typeName);

        bool TryGetInputType<T>(string typeName, out T type)
            where T : IInputType;
    }
}
