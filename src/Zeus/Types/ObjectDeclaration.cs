using System.Collections.Generic;

namespace Zeus.Types
{
    public class ObjectDeclaration
        : ObjectDeclarationBase
    {
        public ObjectDeclaration(string name, IEnumerable<FieldDeclaration> fields)
            : base(name, fields)
        {
        }
    }
}