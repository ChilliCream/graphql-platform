using System.Xml.Linq;
using System.Linq;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public class SchemaMerger
        : ISchemaMerger
    {
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
                ObjectTypeDefinitionNode rootType =
                    schema.GetRootType(operation);
                if (rootType != null)
                {
                    types.Add(new TypeInfo(rootType, schema));
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
            var types = new List<TypeInfo>();

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
            ICollection<TypeInfo> types)
        {
            types.Clear();

            foreach (SchemaInfo schema in schemas)
            {
                if (schema.Types.TryGetValue(name,
                    out ITypeDefinitionNode typeDefinition))
                {
                    types.Add(new TypeInfo(typeDefinition, schema));
                }
            }
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
    }
}
