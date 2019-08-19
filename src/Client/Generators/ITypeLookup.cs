using HotChocolate.Language;

namespace StrawberryShake.Generators
{
    public interface ITypeLookup
    {
        string GetTypeName(SelectionSetNode selectionSet, FieldNode field, bool readOnly);
    }
}
