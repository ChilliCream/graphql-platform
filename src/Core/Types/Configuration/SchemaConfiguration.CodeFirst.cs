using System;
using HotChocolate.Utilities;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
    {
        public void RegisterType<T>()
        {
            Builder.AddType<T>();
        }

        public void RegisterType(Type type)
        {
            Builder.AddType(type);
        }

        public void RegisterQueryType<T>() where T : class
        {
            Builder.AddQueryType<T>();
        }

        public void RegisterQueryType(Type type)
        {
            Builder.AddQueryType(type);
        }

        public void RegisterMutationType<T>() where T : class
        {
            Builder.AddMutationType<T>();
        }

        public void RegisterMutationType(Type type)
        {
            Builder.AddMutationType(type);
        }

        public void RegisterSubscriptionType<T>() where T : class
        {
            Builder.AddSubscriptionType<T>();
        }

        public void RegisterSubscriptionType(Type type)
        {
            Builder.AddSubscriptionType(type);
        }

        public void RegisterType<T>(T namedType)
            where T : class, INamedType
        {
            Builder.AddType(namedType);
        }

        public void RegisterQueryType<T>(T objectType)
            where T : ObjectType
        {
            Builder.AddQueryType(objectType);
        }

        public void RegisterMutationType<T>(T objectType)
            where T : ObjectType
        {
            Builder.AddMutationType(objectType);
        }

        public void RegisterSubscriptionType<T>(T objectType)
            where T : ObjectType
        {
            Builder.AddSubscriptionType(objectType);
        }

        public void RegisterDirective<T>() where T : DirectiveType, new()
        {
            Builder.AddDirectiveType<T>();
        }

        public void RegisterDirective(Type type)
        {
            Builder.AddDirectiveType(type);
        }

        public void RegisterDirective<T>(T directive) where T : DirectiveType
        {
            Builder.AddDirectiveType(directive);
        }
    }
}
