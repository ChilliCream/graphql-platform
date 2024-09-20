namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public interface ICodeContainer<out T>: ICode
{
    public T AddCode(string code, bool addIf = true);

    public T AddCode(ICode code, bool addIf = true);

    public T AddEmptyLine();
}
