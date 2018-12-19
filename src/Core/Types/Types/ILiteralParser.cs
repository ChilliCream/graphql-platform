using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface ILiteralParser
    {
        object ParseLiteral(IValueNode literal);
    }
}
