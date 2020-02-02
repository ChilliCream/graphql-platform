using System;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    public interface ISchemaConfiguration
        : IFluent
    {
        ISchemaOptions Options { get; }

        IBindResolverDelegate BindResolver(
            FieldResolverDelegate fieldResolver);

        IBindResolver<TResolver> BindResolver<TResolver>(
            BindingBehavior bindingBehavior)
            where TResolver : class;

        IBindType<T> BindType<T>(BindingBehavior bindingBehavior)
            where T : class;

        ISchemaConfiguration RegisterIsOfType(IsOfTypeFallback isOfType);

        ISchemaConfiguration RegisterType<T>();

        ISchemaConfiguration RegisterType(Type type);

        ISchemaConfiguration RegisterQueryType(Type type);

        ISchemaConfiguration RegisterQueryType<T>() where T : class;

        ISchemaConfiguration RegisterMutationType<T>() where T : class;

        ISchemaConfiguration RegisterMutationType(Type type);

        ISchemaConfiguration RegisterSubscriptionType<T>() where T : class;

        ISchemaConfiguration RegisterSubscriptionType(Type type);

        ISchemaConfiguration RegisterDirective<T>()
            where T : DirectiveType, new();

        ISchemaConfiguration RegisterDirective(Type type);

        ISchemaConfiguration RegisterType(INamedType namedType);

        ISchemaConfiguration RegisterType(
            INamedTypeExtension namedTypeExtension);

        ISchemaConfiguration RegisterQueryType<T>(T objectType)
            where T : ObjectType;

        ISchemaConfiguration RegisterMutationType<T>(T objectType)
            where T : ObjectType;

        ISchemaConfiguration RegisterSubscriptionType<T>(T objectType)
            where T : ObjectType;

        ISchemaConfiguration RegisterDirective<T>(T directive)
            where T : DirectiveType;

        ISchemaConfiguration Use(FieldMiddleware middleware);

        ISchemaConfiguration RegisterServiceProvider(
            IServiceProvider serviceProvider);

        ISchemaConfigurationExtension Extend();
    }
}
