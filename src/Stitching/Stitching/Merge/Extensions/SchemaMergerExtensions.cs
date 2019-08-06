using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Stitching.Properties;

namespace HotChocolate.Stitching.Merge
{
    public static class SchemaMergerExtensions
    {
        public static ISchemaMerger IgnoreRootTypes(
            this ISchemaMerger schemaMerger)
        {
            if (schemaMerger == null)
            {
                throw new ArgumentNullException(nameof(schemaMerger));
            }

            return schemaMerger.AddDocumentRewriter(
                new RemoveRootTypeRewriter());
        }

        public static ISchemaMerger IgnoreRootTypes(
            this ISchemaMerger schemaMerger,
            NameString schemaName)
        {
            if (schemaMerger == null)
            {
                throw new ArgumentNullException(nameof(schemaMerger));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            return schemaMerger.AddDocumentRewriter(
                new RemoveRootTypeRewriter(schemaName));
        }

        public static ISchemaMerger IgnoreType(
            this ISchemaMerger schemaMerger,
            NameString typeName)
        {
            if (schemaMerger == null)
            {
                throw new ArgumentNullException(nameof(schemaMerger));
            }

            typeName.EnsureNotEmpty(nameof(typeName));

            return schemaMerger.AddDocumentRewriter(
                new RemoveTypeRewriter(typeName));
        }

        public static ISchemaMerger IgnoreType(
            this ISchemaMerger schemaMerger,
            NameString schemaName,
            NameString typeName)
        {
            if (schemaMerger == null)
            {
                throw new ArgumentNullException(nameof(schemaMerger));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            typeName.EnsureNotEmpty(nameof(typeName));

            return schemaMerger.AddDocumentRewriter(
                new RemoveTypeRewriter(schemaName, typeName));
        }

        public static ISchemaMerger IgnoreField(
            this ISchemaMerger schemaMerger,
            FieldReference field)
        {
            if (schemaMerger == null)
            {
                throw new ArgumentNullException(nameof(schemaMerger));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            return schemaMerger.AddTypeRewriter(
                new RemoveFieldRewriter(field));
        }

        public static ISchemaMerger IgnoreField(
            this ISchemaMerger schemaMerger,
            NameString schemaName,
            FieldReference field)
        {
            if (schemaMerger == null)
            {
                throw new ArgumentNullException(nameof(schemaMerger));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            return schemaMerger.AddTypeRewriter(
                new RemoveFieldRewriter(schemaName, field));
        }

        public static ISchemaMerger RenameType(
            this ISchemaMerger schemaMerger,
            NameString originalTypeName,
            NameString newTypeName)
        {
            if (schemaMerger == null)
            {
                throw new ArgumentNullException(nameof(schemaMerger));
            }

            originalTypeName.EnsureNotEmpty(nameof(originalTypeName));
            newTypeName.EnsureNotEmpty(nameof(newTypeName));

            return schemaMerger.AddTypeRewriter(
                new RenameTypeRewriter(originalTypeName, newTypeName));
        }

        public static ISchemaMerger RenameType(
            this ISchemaMerger schemaMerger,
            NameString schemaName,
            NameString originalTypeName,
            NameString newTypeName)
        {
            if (schemaMerger == null)
            {
                throw new ArgumentNullException(nameof(schemaMerger));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            originalTypeName.EnsureNotEmpty(nameof(originalTypeName));
            newTypeName.EnsureNotEmpty(nameof(newTypeName));

            return schemaMerger.AddTypeRewriter(
                new RenameTypeRewriter(
                    schemaName, originalTypeName, newTypeName));
        }

        public static ISchemaMerger RenameField(
            this ISchemaMerger schemaMerger,
            FieldReference field,
            NameString newFieldName)
        {
            if (schemaMerger == null)
            {
                throw new ArgumentNullException(nameof(schemaMerger));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            newFieldName.EnsureNotEmpty(nameof(newFieldName));

            return schemaMerger.AddTypeRewriter(
                new RenameFieldRewriter(
                    field, newFieldName));
        }

        public static ISchemaMerger RenameField(
            this ISchemaMerger schemaMerger,
            NameString schemaName,
            FieldReference field,
            NameString newFieldName)
        {
            if (schemaMerger == null)
            {
                throw new ArgumentNullException(nameof(schemaMerger));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            newFieldName.EnsureNotEmpty(nameof(newFieldName));

            return schemaMerger.AddTypeRewriter(
                new RenameFieldRewriter(
                    schemaName, field, newFieldName));
        }

        [Obsolete("Use AddTypeMergeHandler")]
        public static ISchemaMerger AddMergeHandler<T>(
           this ISchemaMerger merger)
           where T : class, ITypeMergeHandler =>
           AddTypeMergeHandler<T>(merger);

        public static ISchemaMerger AddTypeMergeHandler<T>(
            this ISchemaMerger merger)
            where T : class, ITypeMergeHandler
        {
            if (merger == null)
            {
                throw new System.ArgumentNullException(nameof(merger));
            }

            merger.AddTypeMergeRule(CreateTypeMergeRule<T>());

            return merger;
        }

        public static ISchemaMerger AddDirectiveMergeHandler<T>(
            this ISchemaMerger merger)
            where T : class, IDirectiveMergeHandler
        {
            if (merger == null)
            {
                throw new System.ArgumentNullException(nameof(merger));
            }

            merger.AddDirectiveMergeRule(CreateDirectiveMergeRule<T>());

            return merger;
        }

        internal static MergeTypeRuleFactory CreateTypeMergeRule<T>()
            where T : class, ITypeMergeHandler
        {
            ConstructorInfo constructor =
                CreateHandlerInternal<T, MergeTypeRuleDelegate>();

            return next =>
            {
                var handler = (ITypeMergeHandler)constructor
                    .Invoke(new object[] { next });
                return handler.Merge;
            };
        }

        internal static MergeDirectiveRuleFactory CreateDirectiveMergeRule<T>()
            where T : class, IDirectiveMergeHandler
        {
            ConstructorInfo constructor =
                CreateHandlerInternal<T, MergeDirectiveRuleDelegate>();

            return next =>
            {
                var handler = (IDirectiveMergeHandler)constructor
                    .Invoke(new object[] { next });
                return handler.Merge;
            };
        }

        private static ConstructorInfo CreateHandlerInternal<THandler, TRule>()
            where THandler : class
        {
            ConstructorInfo constructor = typeof(THandler).GetTypeInfo()
                .DeclaredConstructors.SingleOrDefault(c =>
                {
                    ParameterInfo[] parameters = c.GetParameters();
                    return parameters.Length == 1
                        && parameters[0].ParameterType ==
                            typeof(TRule);
                });

            if (constructor == null)
            {
                throw new ArgumentException(StitchingResources
                    .SchemaMergerExtensions_NoValidConstructor);
            }

            return constructor;
        }
    }
}
