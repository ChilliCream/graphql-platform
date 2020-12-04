using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public interface ICodeBuilder
    {
        Task BuildAsync(CodeWriter writer);
    }
}
