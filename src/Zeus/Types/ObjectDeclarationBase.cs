using System;
using System.Collections.Generic;
using System.Linq;

namespace Zeus.Types
{
    public class ObjectDeclarationBase
    {
        protected ObjectDeclarationBase(string name, IEnumerable<FieldDeclaration> fields)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (fields == null)
            {
                throw new System.ArgumentNullException(nameof(fields));
            }

            Name = name;
            Fields = fields.ToDictionary(t => t.Name, StringComparer.Ordinal);
        }

        public string Name { get; }
        public IReadOnlyDictionary<string, FieldDeclaration> Fields { get; }
    }
}