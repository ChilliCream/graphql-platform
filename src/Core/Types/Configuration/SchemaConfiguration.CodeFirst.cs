using System;
using HotChocolate.Utilities;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
    {
        public void RegisterType<T>()
        {
            _builder.AddType<T>();
        }

        public void RegisterType(Type type)
        {
            _builder.AddType(type);
        }

        public void RegisterQueryType<T>() where T : class
        {
            _builder.AddQueryType<T>();
        }

        public void RegisterQueryType(Type type)
        {
            _builder.AddQueryType(type);
        }

        public void RegisterMutationType<T>() where T : class
        {
            _builder.AddMutationType<T>();
        }

        public void RegisterMutationType(Type type)
        {
            _builder.AddMutationType(type);
        }

        public void RegisterSubscriptionType<T>() where T : class
        {
            _builder.AddSubscriptionType<T>();
        }

        public void RegisterSubscriptionType(Type type)
        {
            _builder.AddSubscriptionType(type);
        }

        public void RegisterType<T>(T namedType)
            where T : class, INamedType
        {
            _builder.AddType(namedType);
        }

        public void RegisterQueryType<T>(T objectType)
            where T : ObjectType
        {
            _builder.AddQueryType(objectType);
        }

        public void RegisterMutationType<T>(T objectType)
            where T : ObjectType
        {
            _builder.AddMutationType(objectType);
        }

        public void RegisterSubscriptionType<T>(T objectType)
            where T : ObjectType
        {
            _builder.AddSubscriptionType(objectType);
        }

        public void RegisterDirective<T>() where T : DirectiveType, new()
        {
            _builder.AddDirectiveType<T>();
        }

        public void RegisterDirective(Type type)
        {
            _builder.AddDirectiveType(type);
        }

        public void RegisterDirective<T>(T directive) where T : DirectiveType
        {
            _builder.AddDirectiveType(directive);
        }
    }
}
