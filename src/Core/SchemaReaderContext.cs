using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    public abstract class SchemaReaderContext
    {
        public abstract void Register(IType type);
        public abstract IOutputType GetOutputType(string name);
        public abstract T GetOutputType<T>(string name)
            where T : IOutputType;
        public abstract IInputType GetInputType(string name);
        public abstract FieldResolverDelegate CreateResolver(
            ObjectType objectType, Field field);
        public abstract IsOfType GetTypeChecker(string name);
    }
}