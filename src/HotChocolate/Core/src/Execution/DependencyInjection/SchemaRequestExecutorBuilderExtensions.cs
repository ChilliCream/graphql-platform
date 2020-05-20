using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Execution.Configuration;
using HotChocolate.Configuration.Bindings;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
    /// </summary>
    public static partial class SchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder SetSchema(
            this IRequestExecutorBuilder builder,
            Type schemaType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (schemaType is null)
            {
                throw new ArgumentNullException(nameof(schemaType));
            }

            return builder.ConfigureSchema(b => b.SetSchema(schemaType));
        }

        public static IRequestExecutorBuilder SetSchema(
            this IRequestExecutorBuilder builder,
            ISchema schema)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            return builder.ConfigureSchema(b => b.SetSchema(schema));
        }

        public static IRequestExecutorBuilder SetSchema(
            this IRequestExecutorBuilder builder,
            Action<ISchemaTypeDescriptor> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.SetSchema(configure));
        }

        public static IRequestExecutorBuilder SetOptions(
            this IRequestExecutorBuilder builder,
            IReadOnlySchemaOptions options)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return builder.ConfigureSchema(b => b.SetOptions(options));
        }

        public static IRequestExecutorBuilder ModifyOptions(
            this IRequestExecutorBuilder builder,
            Action<ISchemaOptions> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.ModifyOptions(configure));
        }

        public static IRequestExecutorBuilder UseField(
            this IRequestExecutorBuilder builder,
            FieldMiddleware middleware)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            return builder.ConfigureSchema(b => b.Use(middleware));
        }
       
        public static IRequestExecutorBuilder AddType(
            this IRequestExecutorBuilder builder,
            Type type)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return builder.ConfigureSchema(b => b.AddType(type));
        }

        public static IRequestExecutorBuilder AddType(
            this IRequestExecutorBuilder builder,
            INamedType namedType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (namedType is null)
            {
                throw new ArgumentNullException(nameof(namedType));
            }

            return builder.ConfigureSchema(b => b.AddType(namedType));
        }

        public static IRequestExecutorBuilder AddTypeExtension(
            this IRequestExecutorBuilder builder,
            INamedTypeExtension typeExtension)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (typeExtension is null)
            {
                throw new ArgumentNullException(nameof(typeExtension));
            }

            return builder.ConfigureSchema(b => b.AddType(typeExtension));
        }

        public static IRequestExecutorBuilder BindRuntimeType(
            this IRequestExecutorBuilder builder,
            Type runtimeType,
            Type schemaType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (runtimeType is null)
            {
                throw new ArgumentNullException(nameof(runtimeType));
            }

            if (schemaType is null)
            {
                throw new ArgumentNullException(nameof(schemaType));
            }

            return builder.ConfigureSchema(b => b.BindClrType(runtimeType, schemaType));
        }

        public static IRequestExecutorBuilder AddRootType(
            this IRequestExecutorBuilder builder,
            Type rootType,
            OperationType operation)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (rootType is null)
            {
                throw new ArgumentNullException(nameof(rootType));
            }

            return builder.ConfigureSchema(b => b.AddRootType(rootType, operation));
        }

        public static IRequestExecutorBuilder AddRootType(
            this IRequestExecutorBuilder builder,
            ObjectType rootType,
            OperationType operation)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (rootType is null)
            {
                throw new ArgumentNullException(nameof(rootType));
            }

            return builder.ConfigureSchema(b => b.AddRootType(rootType, operation));
        }

        public static IRequestExecutorBuilder AddDirectiveType(
            this IRequestExecutorBuilder builder,
            DirectiveType directiveType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (directiveType is null)
            {
                throw new ArgumentNullException(nameof(directiveType));
            }

            return builder.ConfigureSchema(b => b.AddDirectiveType(directiveType));
        }

        public static IRequestExecutorBuilder AddResolver(
            this IRequestExecutorBuilder builder,
            FieldResolver fieldResolver)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (fieldResolver is null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(fieldResolver));
        }

        public static IRequestExecutorBuilder SetContextData(
            this IRequestExecutorBuilder builder,
            string key,
            object? value)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return builder.ConfigureSchema(b => b.SetContextData(key, value));
        }
    }
}