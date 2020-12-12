using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class TypeBuilder : ICodeBuilder
    {
        private string? _name;
        private ListType _isList = ListType.NoList;
        private bool _isNullable = false;
        public static TypeBuilder New() => new TypeBuilder();

        public TypeBuilder SetListType(ListType isList)
        {
            _isList = isList;
            return this;
        }

        public TypeBuilder SetName(string name)
        {
            _name = name;
            return this;
        }

        public TypeBuilder SetNullability(bool isNullable)
        {
            _isNullable = isNullable;
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            await writer.WriteAsync(
                $"{_isList.IfListPrint("IReadOnlyList<")}" +
                $"{_name}{_isNullable.IfTruePrint("?")}" +
                $"{_isList.IfListPrint(">")}{_isList.IfNullableListPrint("?")}"
            ).ConfigureAwait(false);
            await writer.WriteSpaceAsync().ConfigureAwait(false);
        }
    }
}
