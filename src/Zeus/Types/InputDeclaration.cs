using System.Collections.Generic;

namespace Zeus.Types
{
    public class InputDeclaration
        : ObjectDeclarationBase
    {
        public InputDeclaration(string name, IEnumerable<FieldDeclaration> fields)
            : base(name, fields)
        {
        }
    }
}