using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public interface ICodeBuilder
    {
        Task BuildAsync(CodeWriter writer);
    }
}
