using System;
using HotChocolate.Configuration;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    public delegate DocumentNode LoadSchemaDocument(IServiceProvider services);

    public interface ISchemaBuilder
    {
        ISchemaBuilder SetDescription(string description);

        ISchemaBuilder SetOptions(IReadOnlySchemaOptions options);

        ISchemaBuilder AddDirective<T>(T directiveInstance)
            where T : class;

        ISchemaBuilder AddDirective<T>()
            where T : class, new();
        ISchemaBuilder AddDirective(
            NameString name,
            params ArgumentNode[] arguments);

        ISchemaBuilder Use(FieldMiddleware middleware);

        ISchemaBuilder AddDocument(
            LoadSchemaDocument loadSchemaDocument);

        ISchemaBuilder AddType(Type type);

        ISchemaBuilder AddType(INamedType type);

        ISchemaBuilder AddRootType(
            Type type,
            OperationType operation);

        ISchemaBuilder AddRootType(
            ObjectType type,
            OperationType operation);

        ISchemaBuilder AddDirectiveType(DirectiveType type);

        ISchemaBuilder AddResolver(FieldResolver fieldResolver);

        ISchemaBuilder AddBinding(IBindingInfo binding);

        ISchemaBuilder SetTypeResolver(IsOfTypeFallback isOfType);

        ISchemaBuilder AddServices(IServiceProvider services);

        ISchema Create();
    }
}
