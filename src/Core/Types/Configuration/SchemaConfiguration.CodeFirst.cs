using System;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
    {
        public ISchemaConfiguration RegisterType<T>()
        {
            _builder.AddType<T>();
            return this;
        }

        public ISchemaConfiguration RegisterType(Type type)
        {
            _builder.AddType(type);
            return this;
        }

        public ISchemaConfiguration RegisterQueryType<T>() where T : class
        {
            _builder.AddQueryType<T>();
            return this;
        }

        public ISchemaConfiguration RegisterQueryType(Type type)
        {
            _builder.AddQueryType(type);
            return this;
        }

        public ISchemaConfiguration RegisterMutationType<T>() where T : class
        {
            _builder.AddMutationType<T>();
            return this;
        }

        public ISchemaConfiguration RegisterMutationType(Type type)
        {
            _builder.AddMutationType(type);
            return this;
        }

        public ISchemaConfiguration RegisterSubscriptionType<T>()
            where T : class
        {
            _builder.AddSubscriptionType<T>();
            return this;
        }

        public ISchemaConfiguration RegisterSubscriptionType(Type type)
        {
            _builder.AddSubscriptionType(type);
            return this;
        }

        public ISchemaConfiguration RegisterType(INamedType namedType)
        {
            _builder.AddType(namedType);
            return this;
        }

        public ISchemaConfiguration RegisterType(
            INamedTypeExtension namedTypeExtension)
        {
            _builder.AddType(namedTypeExtension);
            return this;
        }

        public ISchemaConfiguration RegisterQueryType<T>(T objectType)
            where T : ObjectType
        {
            _builder.AddQueryType(objectType);
            return this;
        }

        public ISchemaConfiguration RegisterMutationType<T>(T objectType)
            where T : ObjectType
        {
            _builder.AddMutationType(objectType);
            return this;
        }

        public ISchemaConfiguration RegisterSubscriptionType<T>(T objectType)
            where T : ObjectType
        {
            _builder.AddSubscriptionType(objectType);
            return this;
        }

        public ISchemaConfiguration RegisterDirective<T>()
            where T : DirectiveType, new()
        {
            _builder.AddDirectiveType<T>();
            return this;
        }

        public ISchemaConfiguration RegisterDirective(Type type)
        {
            _builder.AddDirectiveType(type);
            return this;
        }

        public ISchemaConfiguration RegisterDirective<T>(T directive)
            where T : DirectiveType
        {
            _builder.AddDirectiveType(directive);
            return this;
        }
    }
}
