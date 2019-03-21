using System;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
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

        ISchemaBuilder AddType(ITypeSystemObject type);

        ISchemaBuilder AddRootType(
            Type type,
            OperationType operation);

        ISchemaBuilder AddRootType(
            ObjectType type,
            OperationType operation);

        ISchemaBuilder AddResolver(FieldResolver fieldResolver);

        // ISchemaBuilder AddBinding(object binding);

        ISchemaBuilder AddServices(IServiceProvider services);

        ISchema Create();
    }
}
