using System.Diagnostics.Contracts;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    public class SchemaMergeContext
        : ISchemaMergeContext
    {
        private readonly Dictionary<NameString, ITypeDefinitionNode> _types =
            new Dictionary<NameString, ITypeDefinitionNode>();
        private readonly Dictionary<NameString, DirectiveDefinitionNode> _dirs =
            new Dictionary<NameString, DirectiveDefinitionNode>();

        public void AddType(ITypeDefinitionNode type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_types.ContainsKey(type.Name.Value))
            {
                throw new ArgumentException(
                    "A type with that name was already added.");
            }

            _types.Add(type.Name.Value, type);
        }

        public void AddDirective(DirectiveDefinitionNode directive)
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            if (_dirs.ContainsKey(directive.Name.Value))
            {
                throw new ArgumentException(
                    "A type with that name was already added.");
            }

            _dirs.Add(directive.Name.Value, directive);
        }

        public bool ContainsType(NameString typeName)
        {
            typeName.EnsureNotEmpty(nameof(typeName));
            return _types.ContainsKey(typeName);
        }

        public bool ContainsDirective(NameString directiveName)
        {
            directiveName.EnsureNotEmpty(nameof(directiveName));
            return _dirs.ContainsKey(directiveName);
        }

        public DocumentNode CreateSchema()
        {
            var definitions = new List<IDefinitionNode>();
            definitions.AddRange(_types.Values);
            definitions.AddRange(_dirs.Values);
            return new DocumentNode(definitions);
        }
    }
}
