using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public class SchemaMergeContext
        : ISchemaMergeContext
    {
        public Dictionary<NameString, ITypeDefinitionNode> _types =
            new Dictionary<NameString, ITypeDefinitionNode>();

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

        public DocumentNode CreateSchema()
        {
            return new DocumentNode(_types.Values.ToArray());
        }
    }
}
