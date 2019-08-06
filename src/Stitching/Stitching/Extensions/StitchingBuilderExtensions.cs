using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Introspection;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Stitching.Properties;
using HotChocolate.Stitching.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching
{
    public static class StitchingBuilderExtensions
    {
        public static IStitchingBuilder AddSchema(
            this IStitchingBuilder builder,
            NameString name,
            ISchema schema)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            name.EnsureNotEmpty(nameof(name));

            builder.AddQueryExecutor(name, s => schema.MakeExecutable());

            return builder;
        }

        public static IStitchingBuilder AddSchemaFromString(
            this IStitchingBuilder builder,
            NameString name,
            string schema)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(schema))
            {
                throw new ArgumentException(
                    StitchingResources.Schema_EmptyOrNull,
                    nameof(schema));
            }

            name.EnsureNotEmpty(nameof(name));

            builder.AddSchema(name, s => Utf8GraphQLParser.Parse(schema));
            return builder;
        }

        public static IStitchingBuilder AddSchemaFromFile(
            this IStitchingBuilder builder,
            NameString name,
            string path)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(
                    StitchingResources.SchemaFilePath_EmptyOrNull,
                    nameof(path));
            }

            name.EnsureNotEmpty(nameof(name));

            builder.AddSchema(name, s =>
                Utf8GraphQLParser.Parse(File.ReadAllBytes(path)));
            return builder;
        }

        public static IStitchingBuilder AddSchemaFromHttp(
            this IStitchingBuilder builder,
            NameString name)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            name.EnsureNotEmpty(nameof(name));

            builder.AddSchema(name, s =>
            {
                HttpClient httpClient =
                    s.GetRequiredService<IHttpClientFactory>()
                        .CreateClient(name);
                return IntrospectionClient.LoadSchema(httpClient);
            });

            return builder;
        }

        public static IStitchingBuilder AddExtensionsFromFile(
            this IStitchingBuilder builder,
            string path)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(
                    StitchingResources.ExtensionsFilePath_EmptyOrNull,
                    nameof(path));
            }

            builder.AddExtensions(s =>
                Utf8GraphQLParser.Parse(
                    File.ReadAllBytes(path)));
            return builder;
        }

        public static IStitchingBuilder AddExtensionsFromString(
            this IStitchingBuilder builder,
            string extensions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(extensions))
            {
                throw new ArgumentException(
                    StitchingResources.Extensions_EmptyOrNull,
                    nameof(extensions));
            }

            builder.AddExtensions(s => Utf8GraphQLParser.Parse(extensions));
            return builder;
        }

        public static IStitchingBuilder IgnoreRootTypes(
           this IStitchingBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddDocumentRewriter(
                new RemoveRootTypeRewriter());
        }

        public static IStitchingBuilder IgnoreRootTypes(
            this IStitchingBuilder builder,
            NameString schemaName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            return builder.AddDocumentRewriter(
                new RemoveRootTypeRewriter(schemaName));
        }

        public static IStitchingBuilder IgnoreType(
            this IStitchingBuilder builder,
            NameString typeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            typeName.EnsureNotEmpty(nameof(typeName));

            return builder.AddDocumentRewriter(
                new RemoveTypeRewriter(typeName));
        }

        public static IStitchingBuilder IgnoreType(
            this IStitchingBuilder builder,
            NameString schemaName,
            NameString typeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            typeName.EnsureNotEmpty(nameof(typeName));

            return builder.AddDocumentRewriter(
                new RemoveTypeRewriter(schemaName, typeName));
        }

        public static IStitchingBuilder IgnoreField(
            this IStitchingBuilder builder,
            NameString schemaName,
            NameString typeName,
            NameString fieldName) =>
            IgnoreField(builder, schemaName,
                new FieldReference(typeName, fieldName));

        public static IStitchingBuilder IgnoreField(
            this IStitchingBuilder builder,
            NameString schemaName,
            FieldReference field)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            return builder.AddTypeRewriter(
                new RemoveFieldRewriter(schemaName, field));
        }

        public static IStitchingBuilder RenameType(
            this IStitchingBuilder builder,
            NameString originalTypeName,
            NameString newTypeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            originalTypeName.EnsureNotEmpty(nameof(originalTypeName));
            newTypeName.EnsureNotEmpty(nameof(newTypeName));

            return builder.AddTypeRewriter(
                new RenameTypeRewriter(originalTypeName, newTypeName));
        }

        public static IStitchingBuilder RenameType(
            this IStitchingBuilder builder,
            NameString schemaName,
            NameString originalTypeName,
            NameString newTypeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            originalTypeName.EnsureNotEmpty(nameof(originalTypeName));
            newTypeName.EnsureNotEmpty(nameof(newTypeName));

            return builder.AddTypeRewriter(
                new RenameTypeRewriter(
                    schemaName, originalTypeName, newTypeName));
        }

        public static IStitchingBuilder RenameField(
            this IStitchingBuilder builder,
            NameString typeName,
            NameString fieldName,
            NameString newFieldName) =>
            RenameField(builder,
                new FieldReference(typeName, fieldName),
                newFieldName);

        public static IStitchingBuilder RenameField(
            this IStitchingBuilder builder,
            FieldReference field,
            NameString newFieldName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            newFieldName.EnsureNotEmpty(nameof(newFieldName));

            return builder.AddTypeRewriter(
                new RenameFieldRewriter(
                    field, newFieldName));
        }

        public static IStitchingBuilder RenameField(
            this IStitchingBuilder builder,
            NameString schemaName,
            NameString typeName,
            NameString fieldName,
            NameString newFieldName) =>
            RenameField(builder,
                schemaName,
                new FieldReference(typeName, fieldName),
                newFieldName);

        public static IStitchingBuilder RenameField(
            this IStitchingBuilder builder,
            NameString schemaName,
            FieldReference field,
            NameString newFieldName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            newFieldName.EnsureNotEmpty(nameof(newFieldName));

            return builder.AddTypeRewriter(
                new RenameFieldRewriter(
                    schemaName, field, newFieldName));
        }

        public static IStitchingBuilder RenameFieldArgument(
            this IStitchingBuilder builder,
            NameString typeName,
            NameString fieldName,
            NameString argumentName,
            NameString newArgumentName) =>
            RenameField(builder,
                new FieldReference(typeName, fieldName),
                argumentName,
                newArgumentName);

        public static IStitchingBuilder RenameField(
            this IStitchingBuilder builder,
            FieldReference field,
            NameString argumentName,
            NameString newArgumentName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            argumentName.EnsureNotEmpty(nameof(argumentName));
            newArgumentName.EnsureNotEmpty(nameof(newArgumentName));

            return builder.AddTypeRewriter(
                new RenameFieldArgumentRewriter(
                    field,
                    argumentName,
                    newArgumentName));
        }

        public static IStitchingBuilder RenameField(
            this IStitchingBuilder builder,
            NameString schemaName,
            NameString typeName,
            NameString fieldName,
            NameString argumentName,
            NameString newArgumentName) =>
            RenameField(builder,
                schemaName,
                new FieldReference(typeName, fieldName),
                argumentName,
                newArgumentName);

        public static IStitchingBuilder RenameField(
            this IStitchingBuilder builder,
            NameString schemaName,
            FieldReference field,
            NameString argumentName,
            NameString newArgumentName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            argumentName.EnsureNotEmpty(nameof(argumentName));
            newArgumentName.EnsureNotEmpty(nameof(newArgumentName));

            return builder.AddTypeRewriter(
                new RenameFieldArgumentRewriter(
                    schemaName,
                    field,
                    argumentName,
                    newArgumentName));
        }

        public static IStitchingBuilder AddTypeRewriter(
            this IStitchingBuilder builder,
            RewriteTypeDefinitionDelegate rewrite)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (rewrite == null)
            {
                throw new ArgumentNullException(nameof(rewrite));
            }

            return builder.AddTypeRewriter(new DelegateTypeRewriter(rewrite));
        }

        public static IStitchingBuilder AddDocumentRewriter(
            this IStitchingBuilder builder,
            RewriteDocumentDelegate rewrite)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (rewrite == null)
            {
                throw new ArgumentNullException(nameof(rewrite));
            }

            return builder.AddDocumentRewriter(
                new DelegateDocumentRewriter(rewrite));
        }

        [Obsolete("Use AddTypeMergeHandler")]
        public static IStitchingBuilder AddMergeHandler<T>(
            this IStitchingBuilder builder)
            where T : class, ITypeMergeHandler =>
            AddTypeMergeHandler<T>(builder);

        public static IStitchingBuilder AddTypeMergeHandler<T>(
            this IStitchingBuilder builder)
            where T : class, ITypeMergeHandler
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddTypeMergeRule(
                SchemaMergerExtensions.CreateTypeMergeRule<T>());
        }

        public static IStitchingBuilder AddDirectiveMergeHandler<T>(
            this IStitchingBuilder builder)
            where T : class, IDirectiveMergeHandler
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddDirectiveMergeRule(
                SchemaMergerExtensions.CreateDirectiveMergeRule<T>());
        }
    }
}
