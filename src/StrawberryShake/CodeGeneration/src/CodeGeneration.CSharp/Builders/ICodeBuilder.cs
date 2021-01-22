using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public interface ICodeBuilder
    {
        void Build(CodeWriter writer);
    }
}
