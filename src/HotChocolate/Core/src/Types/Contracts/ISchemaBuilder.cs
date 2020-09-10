using System;
using HotChocolate.Configuration;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate
{
    public delegate DocumentNode LoadSchemaDocument(IServiceProvider services);

    public delegate IConvention CreateConvention(IServiceProvider services);

    public interface ISchemaBuilder
    {
        ISchemaBuilder SetSchema(Type type);

        ISchemaBuilder SetSchema(ISchema schema);

        ISchemaBuilder SetSchema(Action<ISchemaTypeDescriptor> configure);

        ISchemaBuilder SetOptions(IReadOnlySchemaOptions options);

        ISchemaBuilder ModifyOptions(Action<ISchemaOptions> configure);

        ISchemaBuilder Use(FieldMiddleware middleware);

        ISchemaBuilder AddDocument(LoadSchemaDocument loadDocument);

        ISchemaBuilder AddType(Type type);

        ISchemaBuilder AddType(INamedType type);

        ISchemaBuilder AddType(INamedTypeExtension type);

        ISchemaBuilder BindClrType(Type clrType, Type schemaType);

        ISchemaBuilder AddRootType(Type type, OperationType operation);

        ISchemaBuilder AddRootType(ObjectType type, OperationType operation);

        ISchemaBuilder AddDirectiveType(DirectiveType type);

        ISchemaBuilder AddResolver(FieldResolver fieldResolver);

        ISchemaBuilder AddBinding(IBindingInfo binding);

        ISchemaBuilder SetTypeResolver(IsOfTypeFallback isOfType);

        ISchemaBuilder AddServices(IServiceProvider services);

        ISchemaBuilder SetContextData(string key, object? value);

        ISchemaBuilder SetContextData(string key, Func<object?, object?> update);

        ISchemaBuilder AddTypeInterceptor(Type interceptor);

        ISchemaBuilder TryAddTypeInterceptor(Type interceptor);

        ISchemaBuilder AddTypeInterceptor(ITypeInitializationInterceptor interceptor);

        ISchemaBuilder TryAddTypeInterceptor(ITypeInitializationInterceptor interceptor);

        ISchemaBuilder AddConvention(
            Type convention,
            CreateConvention factory,
            string? scope = null);

        ISchemaBuilder TryAddConvention(
            Type convention,
            CreateConvention factory,
            string? scope = null);

        ISchemaBuilder OnBeforeCreate(Action<IDescriptorContext> action);

        ISchema Create();
    }
}
