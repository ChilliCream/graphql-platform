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
                SchemaMergerExtensions.CreateHandler<EnumTypeMergeHandler>(),
                SchemaMergerExtensions.CreateHandler<UnionTypeMergeHandler>(),
            };
        private List<MergeTypeHandler> _handlers = new List<MergeTypeHandler>();
        private OrderedDictionary<string, DocumentNode> _schemas =
            new OrderedDictionary<string, DocumentNode>();

        public ISchemaMerger AddHandler(MergeTypeHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handlers.Add(handler);
            return this;
        }

        public ISchemaMerger AddSchema(string name, DocumentNode schema)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The schema name mustn't be null or empty.",
                    nameof(name));
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            _schemas.Add(name, schema);

            return this;
        }

        public DocumentNode Merge()
        {
            MergeTypeDelegate merge = CompileMergeDelegate();
            List<SchemaInfo> schemas = _schemas
                .Select(t => new SchemaInfo(t.Key, t.Value))
                .ToList();
            ISet<string> typeNames = CreateNameSet(schemas);
            var types = new List<TypeInfo>();

            var context = new SchemaMergeContext();

            SetTypes(OperationType.Query, types, schemas);
            merge(context, types);

            foreach (string typeName in typeNames)
            {
                SetTypes(typeName, types, schemas);
                merge(context, types);
            }

            return context.CreateSchema();
        }

        private void MergeRootType(OperationType operationType, IEnumerable<SchemaInfo> schemas,)
        {

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
            ICollection<TypeInfo> types,
            IEnumerable<SchemaInfo> schemas)
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

        private void SetTypes(
            OperationType operation,
            ICollection<TypeInfo> types,
            IEnumerable<SchemaInfo> schemas)
        {
            types.Clear();

            foreach (SchemaInfo schema in schemas)
            {

            }
        }

        private MergeTypeDelegate CompileMergeDelegate()
        {
            MergeTypeDelegate current = (c, t) =>
            {
                throw new NotSupportedException(
                    "The type definitions could not be handled.");
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
