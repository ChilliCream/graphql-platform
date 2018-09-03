using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    public interface ICodeFirstConfiguration
        : IFluent
    {
        void RegisterType<T>()
            where T : class, INamedType;

        void RegisterQueryType<T>()
            where T : class;

        void RegisterMutationType<T>()
            where T : class;

        void RegisterSubscriptionType<T>()
            where T : class;

        void RegisterDirective<T>()
            where T : DirectiveType;

        void RegisterType<T>(T namedType)
            where T : class, INamedType;

        void RegisterQueryType<T>(T objectType)
            where T : ObjectType;

        void RegisterMutationType<T>(T objectType)
            where T : ObjectType;

        void RegisterSubscriptionType<T>(T objectType)
            where T : ObjectType;

        void RegisterDirective<T>(T directive)
            where T : DirectiveType;
    }
}
