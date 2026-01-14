namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public interface ICodeContainer<out T> : ICode
{
    T AddCode(string code, bool addIf = true);

    T AddCode(ICode code, bool addIf = true);

    T AddEmptyLine();
}
