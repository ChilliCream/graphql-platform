using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    // TODO : work in progress new schmea builder interface
    internal interface ISchemaBuilder
    {
        ISchemaBuilder Use(FieldMiddleware middleware);

        ISchemaBuilder AddSource(string sourceText);

        ISchemaBuilder AddType(Type type);

        ISchemaBuilder AddRootType(Type type, OperationType operation);

        ISchemaBuilder AddType(INamedType type);

        ISchemaBuilder AddRootType(ObjectType type, OperationType operation);

        ISchemaBuilder AddResolver(FieldResolver resolver);

        ISchemaBuilder AddBinding(object binding);

        ISchemaBuilder AddServices(IServiceProvider services);

        ISchema Create();
    }

    internal class SchemaBuilder
        : ISchemaBuilder
    {
        public ISchemaBuilder AddBinding(object binding)
        {
            throw new NotImplementedException();
        }

        public ISchemaBuilder AddResolver(FieldResolver resolver)
        {
            throw new NotImplementedException();
        }

        public ISchemaBuilder AddRootType(Type type, OperationType operation)
        {
            throw new NotImplementedException();
        }

        public ISchemaBuilder AddRootType(ObjectType type, OperationType operation)
        {
            throw new NotImplementedException();
        }

        public ISchemaBuilder AddServices(IServiceProvider services)
        {
            throw new NotImplementedException();
        }

        public ISchemaBuilder AddSource(string sourceText)
        {
            throw new NotImplementedException();
        }

        public ISchemaBuilder AddType(Type type)
        {
            throw new NotImplementedException();
        }

        public ISchemaBuilder AddType(INamedType type)
        {
            throw new NotImplementedException();
        }

        public ISchema Create()
        {
            throw new NotImplementedException();
        }

        public ISchemaBuilder Use(FieldMiddleware middleware)
        {
            throw new NotImplementedException();
        }

        public static ISchemaBuilder New()
        {
            return new SchemaBuilder();
        }
    }

    internal static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddQueryType(
            this ISchemaBuilder builder,
            Type type)
        {
            return builder.AddRootType(type, OperationType.Query);
        }

        public static ISchemaBuilder AddQueryType(
            this ISchemaBuilder builder,
            ObjectType queryType)
        {
            return builder.AddRootType(queryType, OperationType.Query);
        }

        public static ISchemaBuilder AddQueryType<TQuery>(
            this ISchemaBuilder builder)
        {
            return builder.AddRootType(typeof(TQuery), OperationType.Query);
        }
    }

    public static class Foo
    {
        public static void Configure()
        {
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Bar>()
                .AddSource("")
                .Use(next => context => next(context))
                .Create();
        }
    }

    public class Bar { }
}
