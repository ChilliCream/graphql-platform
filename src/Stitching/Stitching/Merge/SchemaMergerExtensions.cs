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

            return schemaMerger.AddRewriter(
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

            return schemaMerger.AddRewriter(
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

            return schemaMerger.AddRewriter(
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

            return schemaMerger.AddRewriter(
                new RemoveTypeRewriter(schemaName, typeName));
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

            return schemaMerger.AddRewriter(
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

            return schemaMerger.AddRewriter(
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

            return schemaMerger.AddRewriter(
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

            return schemaMerger.AddRewriter(
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

            return schemaMerger.AddRewriter(
                new RenameFieldRewriter(
                    schemaName, field, newFieldName));
        }

        public static ISchemaMerger AddMergeHandler<T>(
           this ISchemaMerger merger)
           where T : ITypeMergeHanlder
        {
            if (merger == null)
            {
                throw new System.ArgumentNullException(nameof(merger));
            }

            merger.AddMergeHandler(CreateHandler<T>());

            return merger;
        }

        internal static MergeTypeHandler CreateHandler<T>()
            where T : ITypeMergeHanlder
        {
            ConstructorInfo constructor = typeof(T).GetTypeInfo()
                .DeclaredConstructors.SingleOrDefault(c =>
                {
                    ParameterInfo[] parameters = c.GetParameters();
                    return parameters.Length == 1
                        && parameters[0].ParameterType ==
                            typeof(MergeTypeDelegate);
                });

            if (constructor == null)
            {
                throw new ArgumentException(
                    Resources.SchemaMergerExtensions_NoValidConstructor);
            }

            return next =>
            {
                ITypeMergeHanlder handler = (ITypeMergeHanlder)constructor
                    .Invoke(new object[] { next });
                return handler.Merge;
            };
        }
    }
}
