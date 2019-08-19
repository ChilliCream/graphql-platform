using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class TypeRegistry
        : ITypeLookup
    {
        public string GetTypeName(SelectionSetNode selectionSet, FieldNode field, bool readOnly)
        {
            throw new System.NotImplementedException();
        }

        public void AddTypeName(SelectionSetNode selectionSet, IOutputField field, FieldNode selection, string typeName)
        {

        }

        private string CreateCSharpTypeExpression(IType type)
        {
            if (type is NonNullType nnt)
            {
                return CreateCSharpTypeExpression(nnt.Type);
            }

            if (type is ListType lt)
            {
                return System.Collections.Generic.IReadOnlyList < CreateCSharpTypeExpression()
            }
        }
    }
}
