using System.Xml.Linq;
using System.Linq;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Execution;
using HotChocolate.Stitching.Merge.Handlers;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Merge
{
    public class SchemaMerger
        : ISchemaMerger
    {
        private delegate T RewriteFieldsDelegate<T>(
            IReadOnlyList<FieldDefinitionNode> fields)
            where T : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode;

        private static List<MergeTypeHandler> _defaultHandlers =
            new List<MergeTypeHandler>
            {
                SchemaMergerExtensions
                    .CreateHandler<ScalarTypeMergeHandler>(),
                SchemaMergerExtensions
                    .CreateHandler<InputObjectTypeMergeHandler>(),
                SchemaMergerExtensions
                    .CreateHandler<RootTypeMergeHandler>(),
                SchemaMergerExtensions
                    .CreateHandler<ObjectTypeMergeHandler>(),
                SchemaMergerExtensions
                    .CreateHandler<InterfaceTypeMergeHandler>(),
                SchemaMergerExtensions
                    .CreateHandler<UnionTypeMergeHandler>(),
                SchemaMergerExtensions
                    .CreateHandler<EnumTypeMergeHandler>(),
            };
        private List<MergeTypeHandler> _handlers = new List<MergeTypeHandler>();
        private OrderedDictionary<NameString, DocumentNode> _schemas =
            new OrderedDictionary<NameString, DocumentNode>();
        private Dictionary<NameString, ISet<NameString>> _ignoredTypes =
            new Dictionary<NameString, ISet<NameString>>();
        private Dictionary<NameString, IgnoredFields> _ignoreFields =
            new Dictionary<NameString, IgnoredFields>();
        private Dictionary<NameString, Renamed> _renameType =
            new Dictionary<NameString, Renamed>();
        private Dictionary<NameString, RenamedFields> _renameFields =
            new Dictionary<NameString, RenamedFields>();
        private HashSet<NameString> _ignoredRootTypes =
            new HashSet<NameString>();

        public ISchemaMerger AddMergeHandler(MergeTypeHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handlers.Add(handler);
            return this;
        }

        public ISchemaMerger AddSchema(NameString name, DocumentNode schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            name.EnsureNotEmpty(nameof(name));

            _schemas.Add(name, schema);

            return this;
        }

        public ISchemaMerger IgnoreRootTypes(NameString schemaName)
        {
            schemaName.EnsureNotEmpty(nameof(schemaName));
            _ignoredRootTypes.Add(schemaName);
            return this;
        }

        public ISchemaMerger IgnoreType(
            NameString schemaName,
            NameString typeName)
        {
            schemaName.EnsureNotEmpty(nameof(schemaName));
            typeName.EnsureNotEmpty(nameof(typeName));

            if (!_ignoredTypes.TryGetValue(typeName, out ISet<NameString> set))
            {
                set = new HashSet<NameString>();
                _ignoredTypes[typeName] = set;
            }
            set.Add(typeName);

            return this;
        }

        public ISchemaMerger IgnoreField(
            NameString schemaName,
            FieldReference field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            if (!_ignoreFields.TryGetValue(schemaName, out IgnoredFields types))
            {
                types = new IgnoredFields();
                _ignoreFields[schemaName] = types;
            }

            if (!types.TryGetValue(field.TypeName, out ISet<NameString> fields))
            {
                fields = new HashSet<NameString>();
                types[field.TypeName] = fields;
            }

            fields.Add(field.FieldName);

            return this;
        }

        public ISchemaMerger RenameType(
            NameString schemaName,
            NameString typeName,
            NameString newName)
        {
            schemaName.EnsureNotEmpty(nameof(schemaName));
            typeName.EnsureNotEmpty(nameof(typeName));
            newName.EnsureNotEmpty(nameof(newName));

            if (!_renameType.TryGetValue(schemaName, out Renamed map))
            {
                map = new Renamed();
                _renameType[schemaName] = map;
            }
            map[typeName] = newName;

            return this;
        }

        public ISchemaMerger RenameField(
            NameString schemaName,
            FieldReference field,
            NameString newName)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            newName.EnsureNotEmpty(nameof(newName));

            if (!_renameFields.TryGetValue(schemaName, out RenamedFields types))
            {
                types = new RenamedFields();
                _renameFields[schemaName] = types;
            }

            if (!types.TryGetValue(field.TypeName, out Renamed fields))
            {
                fields = new Renamed();
                types[field.TypeName] = fields;
            }

            fields[field.FieldName] = newName;

            return this;
        }

        public DocumentNode Merge()
        {
            MergeTypeDelegate merge = CompileMergeDelegate();

            List<SchemaInfo> schemas = _schemas
                .Select(t => new SchemaInfo(t.Key, t.Value))
                .ToList();

            var context = new SchemaMergeContext();

            MergeRootType(context, OperationType.Query, schemas, merge);
            MergeRootType(context, OperationType.Mutation, schemas, merge);
            MergeRootType(context, OperationType.Subscription, schemas, merge);

            MergeTypes(context, CreateNameSet(schemas), schemas, merge);

            // TODO : FIX NAMES

            return context.CreateSchema();
        }

        private void MergeRootType(
            ISchemaMergeContext context,
            OperationType operation,
            IEnumerable<SchemaInfo> schemas,
            MergeTypeDelegate merge)
        {
            var types = new List<TypeInfo>();

            foreach (SchemaInfo schema in schemas)
            {
                if (!_ignoredRootTypes.Contains(schema.Name))
                {
                    ObjectTypeDefinitionNode rootType =
                        schema.GetRootType(operation);
                    if (rootType != null)
                    {
                        types.Add(new ObjectTypeInfo(rootType, schema));
                    }
                }
            }

            if (types.Count > 0)
            {
                merge(context, types);
            }
        }

        private void MergeTypes(
            ISchemaMergeContext context,
            ISet<string> typeNames,
            IEnumerable<SchemaInfo> schemas,
            MergeTypeDelegate merge)
        {
            var types = new List<ITypeInfo>();

            foreach (string typeName in typeNames)
            {
                SetTypes(typeName, schemas, types);
                merge(context, types);
            }
        }

        private ISet<string> CreateNameSet(
            IEnumerable<SchemaInfo> schemas)
        {
            HashSet<string> names = new HashSet<string>();

            foreach (SchemaInfo schema in schemas)
            {
                foreach (string name in schema.Types.Keys)
                {
                    names.Add(name);
                }
            }

            return names;
        }

        private void SetTypes(
            string name,
            IEnumerable<SchemaInfo> schemas,
            ICollection<ITypeInfo> types)
        {
            types.Clear();

            foreach (SchemaInfo schema in schemas)
            {
                if (!IsTypeIgnored(schema.Name, name)
                    && schema.Types.TryGetValue(name,
                        out ITypeDefinitionNode typeDefinition))
                {
                    typeDefinition = RewriteType(schema.Name, typeDefinition);
                    types.Add(TypeInfo.Create(typeDefinition, schema));
                }
            }
        }

        private bool IsTypeIgnored(NameString schemaName, NameString typeName)
        {
            return _ignoredTypes.Count > 0
                && _ignoredTypes.TryGetValue(schemaName,
                    out ISet<NameString> ignoredTypes)
                && ignoredTypes.Contains(typeName);
        }

        private ITypeDefinitionNode RewriteType(
            NameString schemaName,
            ITypeDefinitionNode typeDefinitionNode)
        {
            ITypeDefinitionNode current = typeDefinitionNode;

            if (_renameType.TryGetValue(schemaName, out Renamed map)
                && map.TryGetValue(typeDefinitionNode.Name.Value,
                    out NameString newName))
            {
                current = current.AddSource(newName, schemaName);
            }

            return RewriteFields(schemaName, typeDefinitionNode);
        }

        private ITypeDefinitionNode RewriteFields(
            NameString schemaName,
            ITypeDefinitionNode typeDefinitionNode)
        {
            switch (typeDefinitionNode)
            {
                case InputObjectTypeDefinitionNode iotd:
                    return RenameFields(schemaName,
                        RemoveFields(schemaName, iotd));

                case ObjectTypeDefinitionNode otd:
                    otd = RemoveFields(schemaName, otd,
                        f => otd.WithFields(f));
                    return RenameFields(schemaName, otd,
                        f => otd.WithFields(f));

                case InterfaceTypeDefinitionNode itd:
                    itd = RemoveFields(schemaName, itd,
                        f => itd.WithFields(f));
                    return RenameFields(schemaName, itd,
                        f => itd.WithFields(f));

                default:
                    return typeDefinitionNode;
            }
        }

        private T RenameFields<T>(
            NameString schemaName,
            T typeDefinition,
            RewriteFieldsDelegate<T> rewrite)
            where T : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode
        {
            if (_renameFields.TryGetValue(schemaName, out RenamedFields types)
                && types.TryGetValue(typeDefinition.Name.Value,
                    out Renamed fieldNames))
            {
                var renamedFields = new List<FieldDefinitionNode>();

                foreach (FieldDefinitionNode field in
                    typeDefinition.Fields)
                {
                    if (fieldNames.TryGetValue(field.Name.Value,
                        out NameString newName))
                    {
                        renamedFields.Add(field.AddSource(newName, schemaName));
                    }
                    else
                    {
                        renamedFields.Add(field);
                    }
                }

                return rewrite(renamedFields);
            }

            return typeDefinition;
        }

        private T RemoveFields<T>(
            NameString schemaName,
            T typeDefinition,
            RewriteFieldsDelegate<T> rewrite)
            where T : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode
        {
            if (_ignoreFields.TryGetValue(schemaName, out IgnoredFields types)
                && types.TryGetValue(typeDefinition.Name.Value,
                    out ISet<NameString> ignoredFields))
            {
                var renamedFields = new List<FieldDefinitionNode>();

                foreach (FieldDefinitionNode field in
                    typeDefinition.Fields)
                {
                    if (!ignoredFields.Contains(field.Name.Value))
                    {
                        renamedFields.Add(field);
                    }
                }

                return rewrite(renamedFields);
            }

            return typeDefinition;
        }

        private InputObjectTypeDefinitionNode RenameFields(
            NameString schemaName,
            InputObjectTypeDefinitionNode typeDefinition)
        {
            if (_renameFields.TryGetValue(schemaName, out RenamedFields types)
                && types.TryGetValue(typeDefinition.Name.Value,
                    out Renamed fieldNames))
            {
                var renamedFields = new List<InputValueDefinitionNode>();

                foreach (InputValueDefinitionNode field in
                    typeDefinition.Fields)
                {
                    if (fieldNames.TryGetValue(field.Name.Value,
                        out NameString newName))
                    {
                        renamedFields.Add(field.AddSource(newName, schemaName));
                    }
                    else
                    {
                        renamedFields.Add(field);
                    }
                }

                return typeDefinition.WithFields(renamedFields);
            }

            return typeDefinition;
        }

        private InputObjectTypeDefinitionNode RemoveFields(
            NameString schemaName,
            InputObjectTypeDefinitionNode typeDefinition)
        {
            if (_ignoreFields.TryGetValue(schemaName, out IgnoredFields types)
                && types.TryGetValue(typeDefinition.Name.Value,
                    out ISet<NameString> ignoredFields))
            {
                var renamedFields = new List<InputValueDefinitionNode>();

                foreach (InputValueDefinitionNode field in
                    typeDefinition.Fields)
                {
                    if (!ignoredFields.Contains(field.Name.Value))
                    {
                        renamedFields.Add(field);
                    }
                }

                return typeDefinition.WithFields(renamedFields);
            }

            return typeDefinition;
        }

        private MergeTypeDelegate CompileMergeDelegate()
        {
            MergeTypeDelegate current = (c, t) =>
            {
                if (t.Count > 0)
                {
                    throw new NotSupportedException(
                        "The type definitions could not be handled.");
                }
            };

            var handlers = new List<MergeTypeHandler>();
            handlers.AddRange(_defaultHandlers);
            handlers.AddRange(_handlers);

            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                current = handlers[i].Invoke(current);
            }

            return current;
        }

        public static SchemaMerger New() => new SchemaMerger();

        private class RenamedFields
            : Dictionary<NameString, Renamed>
        {
        }

        private class Renamed
            : Dictionary<NameString, NameString>
        {
        }

        private class IgnoredFields
            : Dictionary<NameString, ISet<NameString>>
        {
        }
    }
}
